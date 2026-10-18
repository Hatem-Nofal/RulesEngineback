using Microsoft.Extensions.Logging;
using RulesEngine.Common.Enums;
using RulesEngine.Integrations;
using RulesEngine.Lookups;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

public partial class RulesValidationManager : IRulesValidationManager, ITransientDependency
{
    /// <summary>Claims are processed in chunks so the per-(claim, workflow) result slots stay small.</summary>
    private const int ClaimChunkSize = 256;

    private readonly ILogger<RulesValidationManager> _logger;
    private readonly IRulesEngineManager _rulesEngineManager;
    private readonly IRepository<Diagnosis> _diagnosisRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public RulesValidationManager(
        ILogger<RulesValidationManager> logger,
        IRulesEngineManager rulesEngineManager,
        IRepository<Diagnosis> diagnosisRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _logger = logger;
        _rulesEngineManager = rulesEngineManager;
        _diagnosisRepository = diagnosisRepository;
        _asyncExecuter = asyncExecuter;
    }

    // =============================================================================================
    // Public API
    // =============================================================================================

    public async Task<ClaimValidationResponse> SpecificValidateClaimsByCodesExtended(
        List<Guid>? workflowIds,
        List<string>? workflownames,
        List<ClaimValidationByCodeRequestExtended> claims,
        CancellationToken cancellationToken = default)
    {
        if (claims == null || claims.Count == 0)
            return new ClaimValidationResponse { Success = "No Claims To Validate" };

        var startedAt = Stopwatch.GetTimestamp();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            LogStarting(
                claims.Count,
                workflowIds != null ? string.Join(",", workflowIds) : "none",
                workflownames != null ? string.Join(",", workflownames) : "none");
        }

        var snapshot = await _rulesEngineManager.GetSnapshotAsync(cancellationToken);
        var plans = ResolveWorkflows(snapshot, workflowIds, workflownames);

        var today = DateTime.Today;
        var claimItems = claims.ConvertAll(claim => ClaimWorkItem.Create(claim, today));

        await EnsureDiagnosisCodesExistAsync(claimItems, cancellationToken);

        var (failures, errors) = await EvaluateAsync(snapshot.Engine, plans, claimItems, cancellationToken);

        var elapsedMs = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (errors.Count > 0)
        {
            LogCompletedWithErrors(elapsedMs, claims.Count, failures.Count, errors.Count, string.Join(" | ", errors));
            throw new BusinessException("ValidationFailed").WithData("errors", errors);
        }

        LogCompleted(elapsedMs, claims.Count, failures.Count);

        return failures.Count > 0
            ? new ClaimValidationResponse { Failed = failures }
            : new ClaimValidationResponse { Success = "No Violation Found!" };
    }

    /// <summary>Egyptian national id (14 digits) to age and gender; (0, "") when it cannot be read.</summary>
    public static (int Age, string Gender) ExtractAgeAndGenderFromNationalId(string? nationalId)
        => ExtractAgeAndGenderFromNationalId(nationalId, DateTime.Today);

    internal static (int Age, string Gender) ExtractAgeAndGenderFromNationalId(string? nationalId, DateTime today)
    {
        const NumberStyles Digits = NumberStyles.None;
        var invalid = (0, string.Empty);

        if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Length != 14)
            return invalid;

        var id = nationalId.AsSpan();

        if (!int.TryParse(id[..1], Digits, CultureInfo.InvariantCulture, out var centuryDigit) || centuryDigit is not (2 or 3) ||
            !int.TryParse(id.Slice(1, 2), Digits, CultureInfo.InvariantCulture, out var yearPart) ||
            !int.TryParse(id.Slice(3, 2), Digits, CultureInfo.InvariantCulture, out var month) ||
            !int.TryParse(id.Slice(5, 2), Digits, CultureInfo.InvariantCulture, out var day) ||
            !int.TryParse(id.Slice(12, 1), Digits, CultureInfo.InvariantCulture, out var genderDigit))
        {
            return invalid;
        }

        var year = (centuryDigit == 2 ? 1900 : 2000) + yearPart;
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return invalid;

        var birthDate = new DateTime(year, month, day);

        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;

        return (age, genderDigit % 2 == 0 ? "Female" : "Male");
    }

    // =============================================================================================
    // Step 1: which workflows run
    // =============================================================================================

    private List<WorkflowPlan> ResolveWorkflows(RulesEngineSnapshot snapshot, List<Guid>? workflowIds, List<string>? workflowNames)
    {
        var selected = new List<WorkflowPlan>();
        var seen = new HashSet<Guid>();

        void Add(WorkflowPlan plan)
        {
            if (seen.Add(plan.Id)) selected.Add(plan);
        }

        // By name (normalized: whitespace collapsed, upper-cased).
        var normalizedNames = (workflowNames ?? new List<string>())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => WorkflowNameNormalizer.Normalize(n)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedNames.Count > 0)
        {
            var missingNames = new List<string>();
            foreach (var name in normalizedNames)
            {
                var matches = snapshot.FindByNormalizedName(name);
                if (matches.Count == 0) missingNames.Add(name);
                foreach (var match in matches) Add(match);
            }

            if (missingNames.Count > 0)
            {
                var joined = string.Join(", ", missingNames);
                LogWorkflowNamesNotFound(joined);
                throw new UserFriendlyException($"Workflow(s) not found: {joined}");
            }
        }

        // By id.
        if (workflowIds is { Count: > 0 })
        {
            var missingIds = new List<Guid>();
            foreach (var id in workflowIds.Distinct())
            {
                if (snapshot.TryGetById(id, out var plan)) Add(plan);
                else missingIds.Add(id);
            }

            if (missingIds.Count > 0)
            {
                var joined = string.Join(",", missingIds);
                LogWorkflowIdsNotFound(joined);
                throw new UserFriendlyException($"Workflow(s) not found: {joined}");
            }
        }

        // Nothing requested explicitly: run every active workflow (existing behaviour).
        if (selected.Count == 0)
        {
            foreach (var plan in snapshot.Workflows) Add(plan);
        }

        var withoutRules = selected.Where(p => !p.HasRules).Select(p => p.Name).ToList();
        if (withoutRules.Count > 0)
            LogWorkflowsWithoutRules(string.Join(",", withoutRules));

        return selected.Where(p => p.HasRules).ToList();
    }

    // =============================================================================================
    // Step 2: diagnosis codes must exist
    // =============================================================================================

    private async Task EnsureDiagnosisCodesExistAsync(List<ClaimWorkItem> claims, CancellationToken cancellationToken)
    {
        var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in claims)
            requested.UnionWith(claim.DiagnosisCodes);

        if (requested.Count == 0)
            return;

        var codes = requested.ToList();
        var query = (await _diagnosisRepository.GetQueryableAsync())
            .Where(d => codes.Contains(d.IqviaCode))
            .Select(d => d.IqviaCode);   // projection: only the code is read, nothing is tracked

        var found = new HashSet<string>(
            (await _asyncExecuter.ToListAsync(query, cancellationToken)).Select(c => c?.Trim().ToUpperInvariant() ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        foreach (var claim in claims)
        {
            var invalid = claim.DiagnosisCodes.Where(c => !found.Contains(c)).Distinct().ToList();
            if (invalid.Count == 0)
                continue;

            var joined = string.Join(", ", invalid);
            LogInvalidDiagnosisCodes(claim.ClaimNumber, joined);
            throw new UserFriendlyException($"Invalid diagnosis codes for claim (ClaimNumber: {claim.ClaimNumber}): {joined}");
        }
    }

    // =============================================================================================
    // Step 3: evaluate
    // =============================================================================================

    /// <summary>
    /// Work is split into (claim, workflow) items, so a single claim across many workflows still uses all cores.
    /// Each item writes to its own slot, so there is no shared collection to contend on and the output order
    /// is deterministic (claim order, then workflow order).
    /// </summary>
    private async Task<(List<ClaimValidationResultDto> Failures, List<string> Errors)> EvaluateAsync(
        RulesEngine engine,
        List<WorkflowPlan> plans,
        List<ClaimWorkItem> claims,
        CancellationToken cancellationToken)
    {
        var failures = new List<ClaimValidationResultDto>();
        var errors = new List<string>();

        if (plans.Count == 0)
            return (failures, errors);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        var slots = new WorkItemOutcome?[Math.Min(claims.Count, ClaimChunkSize) * plans.Count];

        for (var offset = 0; offset < claims.Count; offset += ClaimChunkSize)
        {
            var chunkStart = offset;
            var itemCount = Math.Min(ClaimChunkSize, claims.Count - chunkStart) * plans.Count;
            Array.Clear(slots, 0, itemCount);

            await Parallel.ForAsync(0, itemCount, options, async (item, token) =>
            {
                var claim = claims[chunkStart + item / plans.Count];
                var plan = plans[item % plans.Count];
                slots[item] = await EvaluateWorkflowAsync(engine, plan, claim, token);
            });

            for (var i = 0; i < itemCount; i++)
            {
                if (slots[i] is not { } outcome) continue;
                failures.AddRange(outcome.Failures);
                errors.AddRange(outcome.Errors);
            }
        }

        return (failures, errors);
    }

    private async Task<WorkItemOutcome?> EvaluateWorkflowAsync(
        RulesEngine engine, WorkflowPlan plan, ClaimWorkItem claim, CancellationToken cancellationToken)
    {
        if (plan.RequiresPatientDemographics && claim.Age == 0)
            return null;

        var outcome = new WorkItemOutcome();

        switch (plan.RejectionLevel)
        {
            case RejectionLevel.Activity:
            case RejectionLevel.AllActivity:
                foreach (var activity in claim.Activities)
                {
                    var args = plan.RejectionLevel == RejectionLevel.AllActivity
                        ? claim.CreateAllActivityArgs(activity)
                        : claim.CreateActivityArgs(activity);

                    await RunBatchesAsync(engine, plan, claim, activity.ActivityId, args, outcome, cancellationToken);
                }
                break;

            default: // RejectionLevel.Claim
                await RunBatchesAsync(engine, plan, claim, null, claim.GetClaimLevelArgs(), outcome, cancellationToken);
                break;
        }

        return outcome.IsEmpty ? null : outcome;
    }

    /// <param name="activityId">null for claim-level workflows (reported as ActivityId 0).</param>
    private async ValueTask RunBatchesAsync(
        RulesEngine engine,
        WorkflowPlan plan,
        ClaimWorkItem claim,
        int? activityId,
        RuleParameter[] args,
        WorkItemOutcome outcome,
        CancellationToken cancellationToken)
    {
        foreach (var batch in plan.Batches)
        {
            try
            {
                await batch.EnsureCompiledAsync(engine, args, cancellationToken);
                var results = await engine.ExecuteAllRulesAsync(batch.Name, args, cancellationToken);

                for (var i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    if (result.IsSuccess)
                        continue;

                    var ruleName = result.Rule.RuleName;
                    if (!string.IsNullOrEmpty(result.ExceptionMessage))
                        LogRuleThrew(ruleName, plan.Name, claim.ClaimNumber, activityId, result.ExceptionMessage);

                    outcome.Failures.Add(new ClaimValidationResultDto
                    {
                        ActivityId = activityId ?? 0,
                        ClaimNumber = claim.ClaimNumber,
                        WorkFlowName = plan.Name,
                        NormalizedWorkFlowName = plan.NormalizedName,
                        Rule = ruleName
                    });
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogBatchFailed(ex, plan.Name, batch.Index, claim.ClaimNumber, activityId);

                outcome.Errors.Add(activityId is { } id
                    ? $"Workflow '{plan.Name}' batch {batch.Index} for claim '{claim.ClaimNumber}' activity '{id}' failed: {ex.Message}"
                    : $"Workflow '{plan.Name}' batch {batch.Index} for claim '{claim.ClaimNumber}' failed: {ex.Message}");
            }
        }
    }

    private sealed class WorkItemOutcome
    {
        public List<ClaimValidationResultDto> Failures { get; } = new();
        public List<string> Errors { get; } = new();
        public bool IsEmpty => Failures.Count == 0 && Errors.Count == 0;
    }

    // =============================================================================================
    // Logging (source-generated: no boxing/formatting cost when the level is disabled)
    // =============================================================================================

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Starting extended validation for {ClaimCount} claim(s). WorkflowIds: {WorkflowIds}, WorkflowNames: {WorkflowNames}")]
    private partial void LogStarting(int claimCount, string workflowIds, string workflowNames);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Workflow(s) not found: {MissingNames}")]
    private partial void LogWorkflowNamesNotFound(string missingNames);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Workflow(s) not found. Requested ids: {MissingWorkflowIds}")]
    private partial void LogWorkflowIdsNotFound(string missingWorkflowIds);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
        Message = "Workflow(s) exist but have no rules attached, so they will not produce any results: {WorkflowNames}")]
    private partial void LogWorkflowsWithoutRules(string workflowNames);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Invalid diagnosis codes for claim (ClaimNumber: {ClaimNumber}): {InvalidCodes}")]
    private partial void LogInvalidDiagnosisCodes(string? claimNumber, string invalidCodes);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error,
        Message = "Rule {RuleName} in workflow {WorkflowName} threw during evaluation for claim {ClaimNumber}, activity {ActivityId}: {ExceptionMessage}")]
    private partial void LogRuleThrew(string ruleName, string workflowName, string? claimNumber, int? activityId, string exceptionMessage);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error,
        Message = "Workflow {WorkflowName} batch {BatchIndex} for claim {ClaimNumber} activity {ActivityId} failed")]
    private partial void LogBatchFailed(Exception exception, string workflowName, int batchIndex, string? claimNumber, int? activityId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information,
        Message = "Extended validation completed in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}")]
    private partial void LogCompleted(long ms, int claimCount, int failed);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error,
        Message = "Extended validation completed with errors in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}, Errors: {Errors}. Details: {ErrorDetails}")]
    private partial void LogCompletedWithErrors(long ms, int claimCount, int failed, int errors, string errorDetails);
}
