//using Microsoft.Extensions.Logging;
//using RulesEngine.Common.Enums;
//using RulesEngine.Common.Helper;
//using RulesEngine.Entities.Logers;
//using RulesEngine.Entities.Lookups;
//using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
//using RulesEngine.Integrations;
//using RulesEngine.Integrations.RulesEngineManagers;
//using RulesEngine.Models;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Volo.Abp;
//using Volo.Abp.DependencyInjection;
//using Volo.Abp.Domain.Repositories;
//using Volo.Abp.Linq;
//using static FastExpressionCompiler.ExpressionCompiler;

//namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

//public partial class RulesValidationManager : IRulesValidationManager, ITransientDependency
//{
//    /// <summary>Claims are processed in chunks so the per-(claim, workflow) result slots stay small.</summary>
//    private const int ClaimChunkSize = 256;

//    private readonly ILogger<RulesValidationManager> _logger;
//    private readonly IRulesEngineManager _rulesEngineManager;
//    private readonly IRepository<Diagnosis> _diagnosisRepository;
//    private readonly IRepository<ActivitySubType> _activitySubTypeRepository;
//    private readonly IRepository<RulesEngineExecutionLog> _rulesEngineViolationLogRepository;
//    private readonly IAsyncQueryableExecuter _asyncExecuter;

//    public RulesValidationManager(ILogger<RulesValidationManager> logger, IRulesEngineManager rulesEngineManager, IRepository<Diagnosis> diagnosisRepository, IRepository<ActivitySubType> activitySubTypeRepository, IAsyncQueryableExecuter asyncExecuter, IRepository<RulesEngineExecutionLog> rulesEngineViolationLogRepository)
//    {
//        _logger = logger;
//        _rulesEngineManager = rulesEngineManager;
//        _diagnosisRepository = diagnosisRepository;
//        _activitySubTypeRepository = activitySubTypeRepository;
//        _asyncExecuter = asyncExecuter;
//        _rulesEngineViolationLogRepository = rulesEngineViolationLogRepository;
//    }



//    // =============================================================================================
//    // Public API
//    // =============================================================================================

//    public async Task<ClaimValidationResponse> SpecificValidateClaimsByCodesExtended(
//        List<long>? workflowIds,
//        List<string>? workflownames,
//        List<ClaimValidationByCodeRequestExtended> claims,
//        CancellationToken cancellationToken = default)
//    {
//        if (claims == null || claims.Count == 0)
//            return new ClaimValidationResponse { Success = "No Claims To Validate" };

//        var startedAt = Stopwatch.GetTimestamp();

//        if (_logger.IsEnabled(LogLevel.Warning))
//        {
//            LogStarting(
//                claims.Count,
//                workflowIds != null ? string.Join(",", workflowIds) : "none",
//                workflownames != null ? string.Join(",", workflownames) : "none");
//        }

//        var snapshot = await _rulesEngineManager.GetSnapshotAsync(cancellationToken);
//        var plans = ResolveWorkflows(snapshot, workflowIds, workflownames);

//        var today = DateTime.Today;
//        var activitiesMissingGeneric = claims.SelectMany(x => x.Activities)
//             .Where(a => string.IsNullOrWhiteSpace(a.GenericCode))
//            .Select(a => a.ActivityCode)
//            .Where(code => !string.IsNullOrWhiteSpace(code))
//            .Distinct(StringComparer.OrdinalIgnoreCase)
//            .ToList();
//        if (activitiesMissingGeneric.Any())
//        {
//            claims = await LoadGenericDrug(claims, activitiesMissingGeneric);
//        }

//        var allDiagnosisCodes = claims
//              .SelectMany(c => c.DiagnosisCodes)
//              .Distinct(StringComparer.OrdinalIgnoreCase)
//              .ToList();
//        if (allDiagnosisCodes.Any())
//        {
//            claims = await LoadDiagnosisCodes(claims, allDiagnosisCodes);

//        }
//        var claimItems = claims.ConvertAll(claim => ClaimWorkItem.Create(claim, today));

//        await EnsureDiagnosisCodesExistAsync(claimItems, cancellationToken);

//        var (failures, errors) = await EvaluateAsync(snapshot.Engine, plans, claimItems, cancellationToken);

//        var elapsedMs = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

//        if (errors.Count > 0)
//        {
//            LogCompletedWithErrors(elapsedMs, claims.Count, failures.Count, errors.Count, string.Join(" | ", errors));
//            //throw new BusinessException("ValidationFailed").WithData("errors", errors);
//        }

//        LogCompleted(elapsedMs, claims.Count, failures.Count);

//        return failures.Count > 0
//            ? new ClaimValidationResponse { Failed = failures }
//            : new ClaimValidationResponse { Success = "No Violation Found!" };
//    }


//    private async Task<List<ClaimValidationByCodeRequestExtended>> LoadGenericDrug(List<ClaimValidationByCodeRequestExtended> claims, List<string> activitiesMissingGeneric)
//    {

//        var genericCodesByActivityCode = (await _activitySubTypeRepository.WithDetailsAsync(x => x.GenericDrug))

//            .Where(a => activitiesMissingGeneric.Contains(a.iqvia_code) && a.GenericDrug != null)
//            .Select(a => new
//            {
//                ActivityCode = a.iqvia_code,
//                GenericCode = a.GenericDrug.GenericCode
//            })
//            .ToList()
//            .GroupBy(x => x.ActivityCode, StringComparer.OrdinalIgnoreCase)
//            .ToDictionary(
//                g => g.Key,
//                g => g.First().GenericCode,
//                StringComparer.OrdinalIgnoreCase);

//        foreach (var normalizedClaim in claims)
//        {
//            foreach (var activity in normalizedClaim.Activities)
//            {
//                if (!string.IsNullOrWhiteSpace(activity.GenericCode))
//                    continue;

//                if (genericCodesByActivityCode.TryGetValue(
//                        activity.ActivityCode,
//                        out var genericCode)
//                    && !string.IsNullOrWhiteSpace(genericCode))
//                {
//                    activity.GenericCode = genericCode.Trim().ToUpperInvariant();
//                }
//            }
//        }

//        return claims;


//    }
//    private async Task<List<ClaimValidationByCodeRequestExtended>> LoadDiagnosisCodes(List<ClaimValidationByCodeRequestExtended> claims, List<string> allDiagnosisCodes)
//    {

//        var foundDiag = (await _diagnosisRepository.GetListAsync())
//                  .Where(d => allDiagnosisCodes.Contains(d.iqvia_code))
//                  .Select(d => d.iqvia_code)
//                  .ToList();

//        var foundSet = new HashSet<string>(
//            foundDiag.Select(x => x?.Trim().ToUpperInvariant() ?? string.Empty),
//            StringComparer.OrdinalIgnoreCase);

//        foreach (var nc in claims)
//        {
//            var invalid = nc.DiagnosisCodes.Where(c => !foundSet.Contains(c)).Distinct().ToList();
//            if (invalid.Count > 0)
//            {
//                _logger.LogWarning(
//                    "Invalid diagnosis codes for claim (ClaimNumber: {ClaimNumber}): {InvalidCodes}",
//                    nc.ClaimNumber, string.Join(", ", invalid));
//                throw new UserFriendlyException(
//                    $"Invalid diagnosis codes for claim (ClaimNumber: {nc.ClaimNumber}): {string.Join(", ", invalid)}");
//            }
//        }

//        return claims;


//    }


//    // =============================================================================================
//    // Step 1: which workflows run
//    // =============================================================================================

//    private List<WorkflowPlan> ResolveWorkflows(RulesEngineSnapshot snapshot, List<long>? workflowIds, List<string>? workflowNames)
//    {
//        var selected = new List<WorkflowPlan>();
//        var seen = new HashSet<long>();

//        void Add(WorkflowPlan plan)
//        {
//            if (seen.Add(plan.Id)) selected.Add(plan);
//        }

//        // By name (normalized: whitespace collapsed, upper-cased).
//        var normalizedNames = (workflowNames ?? new List<string>())
//            .Where(n => !string.IsNullOrWhiteSpace(n))
//            .Select(n => WorkflowNameNormalizer.Normalize(n)!)
//            .Distinct(StringComparer.OrdinalIgnoreCase)
//            .ToList();

//        if (normalizedNames.Count > 0)
//        {
//            var missingNames = new List<string>();
//            foreach (var name in normalizedNames)
//            {
//                var matches = snapshot.FindByNormalizedName(name);
//                if (matches.Count == 0) missingNames.Add(name);
//                foreach (var match in matches) Add(match);
//            }

//            if (missingNames.Count > 0)
//            {
//                var joined = string.Join(", ", missingNames);
//                LogWorkflowNamesNotFound(joined);
//                throw new UserFriendlyException($"Workflow(s) not found: {joined}");
//            }
//        }

//        // By id.
//        if (workflowIds is { Count: > 0 })
//        {
//            var missingIds = new List<long>();
//            foreach (var id in workflowIds.Distinct())
//            {
//                if (snapshot.TryGetById(id, out var plan)) Add(plan);
//                else missingIds.Add(id);
//            }

//            if (missingIds.Count > 0)
//            {
//                var joined = string.Join(",", missingIds);
//                LogWorkflowIdsNotFound(joined);
//                throw new UserFriendlyException($"Workflow(s) not found: {joined}");
//            }
//        }

//        // Nothing requested explicitly: run every active workflow (existing behaviour).
//        if (selected.Count == 0)
//        {
//            foreach (var plan in snapshot.Workflows) Add(plan);
//        }

//        var withoutRules = selected.Where(p => !p.HasRules).Select(p => p.Name).ToList();
//        if (withoutRules.Count > 0)
//            LogWorkflowsWithoutRules(string.Join(",", withoutRules));

//        return selected.Where(p => p.HasRules).ToList();
//    }

//    // =============================================================================================
//    // Step 2: diagnosis codes must exist
//    // =============================================================================================

//    private async Task EnsureDiagnosisCodesExistAsync(List<ClaimWorkItem> claims, CancellationToken cancellationToken)
//    {
//        var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//        foreach (var claim in claims)
//            requested.UnionWith(claim.DiagnosisCodes);

//        if (requested.Count == 0)
//            return;

//        var codes = requested.ToList();
//        var query = (await _diagnosisRepository.GetQueryableAsync())
//            .Where(d => codes.Contains(d.iqvia_code))
//            .Select(d => d.iqvia_code);   // projection: only the code is read, nothing is tracked

//        var found = new HashSet<string>(
//            (await _asyncExecuter.ToListAsync(query, cancellationToken)).Select(c => c?.Trim().ToUpperInvariant() ?? string.Empty),
//            StringComparer.OrdinalIgnoreCase);

//        foreach (var claim in claims)
//        {
//            var invalid = claim.DiagnosisCodes.Where(c => !found.Contains(c)).Distinct().ToList();
//            if (invalid.Count == 0)
//                continue;

//            var joined = string.Join(", ", invalid);
//            LogInvalidDiagnosisCodes(claim.ClaimNumber, joined);
//            throw new UserFriendlyException($"Invalid diagnosis codes for claim (ClaimNumber: {claim.ClaimNumber}): {joined}");
//        }
//    }

//    // =============================================================================================
//    // Step 3: evaluate
//    // =============================================================================================

//    /// <summary>
//    /// Work is split into (claim, workflow) items, so a single claim across many workflows still uses all cores.
//    /// Each item writes to its own slot, so there is no shared collection to contend on and the output order
//    /// is deterministic (claim order, then workflow order).
//    /// </summary>
//    private async Task<(List<ClaimValidationResultDto> Failures, List<string> Errors)> EvaluateAsync(
//        RulesEngine engine,
//        List<WorkflowPlan> plans,
//        List<ClaimWorkItem> claims,
//        CancellationToken cancellationToken)
//    {
//        var failures = new List<ClaimValidationResultDto>();
//        var errors = new List<string>();

//        if (plans.Count == 0)
//            return (failures, errors);

//        var options = new ParallelOptions
//        {
//            MaxDegreeOfParallelism = Environment.ProcessorCount,
//            CancellationToken = cancellationToken
//        };

//        var slots = new WorkItemOutcome?[Math.Min(claims.Count, ClaimChunkSize) * plans.Count];

//        for (var offset = 0; offset < claims.Count; offset += ClaimChunkSize)
//        {
//            var rulesEngineViolationLogs = new List<RulesEngineExecutionLog>();

//            var chunkStart = offset;
//            var itemCount = Math.Min(ClaimChunkSize, claims.Count - chunkStart) * plans.Count;
//            Array.Clear(slots, 0, itemCount);

//            await Parallel.ForAsync(0, itemCount, options, async (item, token) =>
//            {
//                var claim = claims[chunkStart + item / plans.Count];
//                var plan = plans[item % plans.Count];
//                slots[item] = await EvaluateWorkflowAsync(engine, plan, claim, token);
//            });

//            for (var i = 0; i < itemCount; i++)
//            {
//                if (slots[i] is not { } outcome) continue;
//                failures.AddRange(outcome.Failures);
//                errors.AddRange(outcome.Errors);
//                rulesEngineViolationLogs.AddRange(outcome.RulesEngineViolationLog);
//            }
//            try
//            {
//                await _rulesEngineViolationLogRepository.InsertManyAsync(rulesEngineViolationLogs);

//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }

//        return (failures, errors);
//    }

//    private async Task<WorkItemOutcome?> EvaluateWorkflowAsync(
//        RulesEngine engine, WorkflowPlan plan, ClaimWorkItem claim, CancellationToken cancellationToken)
//    {
//        // Gender is empty only when the national id could not be read. Checking Age == 0 instead would also skip
//        // valid patients younger than one year.
//        if (plan.RequiresPatientDemographics && claim.Gender.Length == 0)
//            return null;

//        var outcome = new WorkItemOutcome();

//        switch (plan.RejectionLevel)
//        {
//            case RejectionLevel.Activity:

//                foreach (var activity in claim.Activities)
//                {
//                    var args = claim.CreateActivityArgs(activity);

//                    await RunBatchesAsync(engine, plan, claim, activity.ActivityId, args, outcome, cancellationToken);
//                }
//                break;

//            default: // RejectionLevel.Claim
//                await RunBatchesAsync(engine, plan, claim, null, claim.GetClaimLevelArgs(), outcome, cancellationToken);
//                break;
//        }

//        return outcome.IsEmpty ? null : outcome;
//    }

//    /// <param name="activityId">null for claim-level workflows (reported as ActivityId 0).</param>
//    private async ValueTask RunBatchesAsync(
//        RulesEngine engine,
//        WorkflowPlan plan,
//        ClaimWorkItem claim,
//        int? activityId,
//        RuleParameter[] args,
//        WorkItemOutcome outcome,
//        CancellationToken cancellationToken)
//    {
//        foreach (var batch in plan.Batches)
//        {
//            try
//            {
//                await batch.EnsureCompiledAsync(engine, args, cancellationToken);
//                var results = await engine.ExecuteAllRulesAsync(batch.Name, args, cancellationToken);
//                if (results.All(x => x.IsSuccess))
//                {
//                    var result = results.FirstOrDefault();

//                    result.Inputs.TryGetValue("ctx", out var inputs);

//                    outcome.RulesEngineViolationLog.Add(RulesEngineExecutionLog.Create(plan.Name, plan.Id, "", null, "", "",
//                                        claim.ClaimNumber, "", ((ActivityRuleContext)inputs)?.Activity, false));
//                    continue;
//                }
//                var failuresResults = results.Where(x => !x.IsSuccess).ToList();
//                for (var i = 0; i < failuresResults.Count(); i++)
//                {
//                    var result = failuresResults[i];


//                    if (result.IsSuccess)
//                    {
//                        //outcome.RulesEngineViolationLog.Add(RulesEngineExecutionLog.Create(plan.Name, plan.Id, ruleName, ruleId, "", "",
//                        //                      claim.ClaimNumber, result.Rule.Expression, ((ActivityRuleContext)inputs)?.Activity, false));

//                        continue;
//                    }
//                    var ruleName = result.Rule.RuleName;
//                    result.Inputs.TryGetValue("ctx", out var inputs);
//                    var ruleId = long.Parse(result.Rule
//                                      .GetType()
//                                      .GetProperty("RuleId")
//                                      ?.GetValue(result.Rule)
//                                      ?.ToString() ?? "0");
//                    if (!string.IsNullOrEmpty(result.ExceptionMessage))
//                        LogRuleThrew(ruleName, plan.Name, claim.ClaimNumber, activityId, result.ExceptionMessage);
//                    var ruleType = result.Rule.GetType();

//                    var violationType = result.Rule
//                                        .GetType()
//                                        .GetProperty("ViolationType")
//                                        ?.GetValue(result.Rule)
//                                        ?.ToString();


//                    var activityQuantity = RuleHelpers.GetPropertyValue("ActivityQuantity", result.Inputs);
//                    int.TryParse(activityQuantity?.ToString(), out var maxDailyQuantity);

//                    outcome.Failures.Add(new ClaimValidationResultDto
//                    {
//                        ActivityId = activityId ?? 0,
//                        ClaimNumber = claim.ClaimNumber,
//                        WorkFlowName = plan.Name,
//                        NormalizedWorkFlowName = plan.NormalizedName,
//                        Rule = ruleName,
//                        ErrorMessage = RuleHelpers.FormatMessage(result.ExceptionMessage, result.Inputs),
//                        ErrorType = violationType,
//                        MaxDailyQuantity = maxDailyQuantity

//                    });
//                    outcome.RulesEngineViolationLog.Add(RulesEngineExecutionLog.Create(
//                         plan.Name,
//                         plan.Id,
//                         ruleName,
//                         ruleId,
//                         violationType,
//                        RuleHelpers.FormatMessage(result.ExceptionMessage, result.Inputs),
//                         claim.ClaimNumber,
//                        result.Rule.Expression,
//                        ((ActivityRuleContext)inputs)?.Activity, true));
//                }
//            }
//            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
//            {
//                throw;
//            }
//            catch (Exception ex)
//            {
//                LogBatchFailed(ex, plan.Name, batch.Index, claim.ClaimNumber, activityId);

//                outcome.Errors.Add(activityId is { } id
//                    ? $"Workflow '{plan.Name}' batch {batch.Index} for claim '{claim.ClaimNumber}' activity '{id}' failed: {ex.Message}"
//                    : $"Workflow '{plan.Name}' batch {batch.Index} for claim '{claim.ClaimNumber}' failed: {ex.Message}");
//            }
//        }
//    }

//    private sealed class WorkItemOutcome
//    {
//        public List<ClaimValidationResultDto> Failures { get; } = new();
//        public List<string> Errors { get; } = new();
//        public List<RulesEngineExecutionLog> RulesEngineViolationLog { get; } = new();
//        public bool IsEmpty => Failures.Count == 0 && Errors.Count == 0;
//    }

//    // =============================================================================================
//    // Logging (source-generated: no boxing/formatting cost when the level is disabled)
//    // =============================================================================================

//    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
//        Message = "Starting extended validation for {ClaimCount} claim(s). WorkflowIds: {WorkflowIds}, WorkflowNames: {WorkflowNames}")]
//    private partial void LogStarting(int claimCount, string workflowIds, string workflowNames);

//    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Workflow(s) not found: {MissingNames}")]
//    private partial void LogWorkflowNamesNotFound(string missingNames);

//    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Workflow(s) not found. Requested ids: {MissingWorkflowIds}")]
//    private partial void LogWorkflowIdsNotFound(string missingWorkflowIds);

//    [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
//        Message = "Workflow(s) exist but have no rules attached, so they will not produce any results: {WorkflowNames}")]
//    private partial void LogWorkflowsWithoutRules(string workflowNames);

//    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
//        Message = "Invalid diagnosis codes for claim (ClaimNumber: {ClaimNumber}): {InvalidCodes}")]
//    private partial void LogInvalidDiagnosisCodes(string? claimNumber, string invalidCodes);

//    [LoggerMessage(EventId = 6, Level = LogLevel.Error,
//        Message = "Rule {RuleName} in workflow {WorkflowName} threw during evaluation for claim {ClaimNumber}, activity {ActivityId}: {ExceptionMessage}")]
//    private partial void LogRuleThrew(string ruleName, string workflowName, string? claimNumber, int? activityId, string exceptionMessage);

//    [LoggerMessage(EventId = 7, Level = LogLevel.Error,
//        Message = "Workflow {WorkflowName} batch {BatchIndex} for claim {ClaimNumber} activity {ActivityId} failed")]
//    private partial void LogBatchFailed(Exception exception, string workflowName, int batchIndex, string? claimNumber, int? activityId);

//    [LoggerMessage(EventId = 8, Level = LogLevel.Warning,
//        Message = "Extended validation completed in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}")]
//    private partial void LogCompleted(long ms, int claimCount, int failed);

//    [LoggerMessage(EventId = 9, Level = LogLevel.Error,
//        Message = "Extended validation completed with errors in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}, Errors: {Errors}. Details: {ErrorDetails}")]
//    private partial void LogCompletedWithErrors(long ms, int claimCount, int failed, int errors, string errorDetails);






//}


using Microsoft.Extensions.Logging;
using RulesEngine.Common.Dtos;
using RulesEngine.Common.Enums;
using RulesEngine.Common.Helper;
using RulesEngine.Entities.Logers;
using RulesEngine.Entities.Lookups;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Integrations;
using RulesEngine.Integrations.RulesEngineManagers;
using RulesEngine.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using static FastExpressionCompiler.ExpressionCompiler;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

public partial class RulesValidationManager : IRulesValidationManager, ITransientDependency
{
    /// <summary>Claims are processed in chunks so the per-(claim, workflow) result slots stay small.</summary>
    private const int ClaimChunkSize = 256;

    private readonly ILogger<RulesValidationManager> _logger;
    private readonly IRulesEngineManager _rulesEngineManager;
    private readonly IRepository<Diagnosis> _diagnosisRepository;
    private readonly IRepository<ActivitySubType> _activitySubTypeRepository;
    private readonly IRepository<RulesEngineExecutionLog> _rulesEngineViolationLogRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public RulesValidationManager(ILogger<RulesValidationManager> logger, IRulesEngineManager rulesEngineManager, IRepository<Diagnosis> diagnosisRepository, IRepository<ActivitySubType> activitySubTypeRepository, IAsyncQueryableExecuter asyncExecuter, IRepository<RulesEngineExecutionLog> rulesEngineViolationLogRepository)
    {
        _logger = logger;
        _rulesEngineManager = rulesEngineManager;
        _diagnosisRepository = diagnosisRepository;
        _activitySubTypeRepository = activitySubTypeRepository;
        _asyncExecuter = asyncExecuter;
        _rulesEngineViolationLogRepository = rulesEngineViolationLogRepository;
    }



    // =============================================================================================
    // Public API
    // =============================================================================================

    public async Task<ClaimValidationResponse> SpecificValidateClaimsByCodesExtended(
        List<long>? workflowIds,
        List<string>? workflownames,
        List<ClaimValidationByCodeRequestExtended> claims,
        CancellationToken cancellationToken = default)
    {
        if (claims == null || claims.Count == 0)
            return new ClaimValidationResponse { Success = "No Claims To Validate" };

        var startedAt = Stopwatch.GetTimestamp();

        if (_logger.IsEnabled(LogLevel.Warning))
        {
            LogStarting(
                claims.Count,
                workflowIds != null ? string.Join(",", workflowIds) : "none",
                workflownames != null ? string.Join(",", workflownames) : "none");
        }

        var snapshot = await _rulesEngineManager.GetSnapshotAsync(cancellationToken);
        var plans = ResolveWorkflows(snapshot, workflowIds, workflownames);

        var today = DateTime.Today;
        var activitiesMissingGeneric = claims.SelectMany(x => x.Activities)
             .Where(a => string.IsNullOrWhiteSpace(a.GenericCode))
            .Select(a => a.ActivityCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (activitiesMissingGeneric.Any())
        {
            claims = await LoadGenericDrug(claims, activitiesMissingGeneric);
        }

        var claimItems = claims.ConvertAll(claim => ClaimWorkItem.Create(claim, today));

        await EnsureDiagnosisCodesExistAsync(claimItems, cancellationToken);

        var (failures, errors) = await EvaluateAsync(snapshot.Engine, plans, claimItems, cancellationToken);

        var elapsedMs = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (errors.Count > 0)
        {
            LogCompletedWithErrors(elapsedMs, claims.Count, failures.Count, errors.Count, string.Join(" | ", errors));
            //throw new BusinessException("ValidationFailed").WithData("errors", errors);
        }

        LogCompleted(elapsedMs, claims.Count, failures.Count);

        return failures.Count > 0
            ? new ClaimValidationResponse { Failed = failures }
            : new ClaimValidationResponse { Success = "No Violation Found!" };
    }


    private async Task<List<ClaimValidationByCodeRequestExtended>> LoadGenericDrug(List<ClaimValidationByCodeRequestExtended> claims, List<string> activitiesMissingGeneric)
    {

        var genericCodesByActivityCode = (await _activitySubTypeRepository.WithDetailsAsync(x => x.GenericDrug))

            .Where(a => activitiesMissingGeneric.Contains(a.iqvia_code) && a.GenericDrug != null)
            .Select(a => new
            {
                ActivityCode = a.iqvia_code,
                GenericCode = a.GenericDrug.GenericCode
            })
            .ToList()
            .GroupBy(x => x.ActivityCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().GenericCode,
                StringComparer.OrdinalIgnoreCase);

        foreach (var normalizedClaim in claims)
        {
            foreach (var activity in normalizedClaim.Activities)
            {
                if (!string.IsNullOrWhiteSpace(activity.GenericCode))
                    continue;

                if (genericCodesByActivityCode.TryGetValue(
                        activity.ActivityCode,
                        out var genericCode)
                    && !string.IsNullOrWhiteSpace(genericCode))
                {
                    activity.GenericCode = genericCode.Trim().ToUpperInvariant();
                }
            }
        }

        return claims;


    }
    // =============================================================================================
    // Step 1: which workflows run
    // =============================================================================================

    private List<WorkflowPlan> ResolveWorkflows(RulesEngineSnapshot snapshot, List<long>? workflowIds, List<string>? workflowNames)
    {
        var selected = new List<WorkflowPlan>();
        var seen = new HashSet<long>();

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
            var missingIds = new List<long>();
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

    /// <summary>Codes already confirmed to exist, so steady-state requests do not touch the database for them.</summary>
    private static readonly ConcurrentDictionary<string, long> KnownDiagnosisCodes = new(StringComparer.OrdinalIgnoreCase);
    private const long KnownDiagnosisCodeTtlMs = 30 * 60 * 1000;

    private async Task EnsureDiagnosisCodesExistAsync(List<ClaimWorkItem> claims, CancellationToken cancellationToken)
    {
        var now = Environment.TickCount64;

        // Only codes not confirmed recently need a database round trip.
        var unknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in claims)
        {
            foreach (var code in claim.DiagnosisCodes)
            {
                if (!KnownDiagnosisCodes.TryGetValue(code, out var confirmedAt) || now - confirmedAt > KnownDiagnosisCodeTtlMs)
                    unknown.Add(code);
            }
        }

        if (unknown.Count == 0)
            return;

        var codes = unknown.ToList();
        var query = (await _diagnosisRepository.GetQueryableAsync())
            .Where(d => codes.Contains(d.iqvia_code))
            .Select(d => d.iqvia_code);   // projection: only the code is read, nothing is tracked

        foreach (var found in await _asyncExecuter.ToListAsync(query, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(found))
                KnownDiagnosisCodes[found.Trim().ToUpperInvariant()] = now;
        }

        foreach (var claim in claims)
        {
            var invalid = claim.DiagnosisCodes.Where(c => !KnownDiagnosisCodes.ContainsKey(c)).Distinct().ToList();
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
            var rulesEngineViolationLogs = new List<RulesEngineExecutionLog>();

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
                rulesEngineViolationLogs.AddRange(outcome.RulesEngineViolationLog);
            }

            if (rulesEngineViolationLogs.Count > 0)
                await _rulesEngineViolationLogRepository.InsertManyAsync(rulesEngineViolationLogs);
        }

        return (failures, errors);
    }

    private async Task<WorkItemOutcome?> EvaluateWorkflowAsync(
        RulesEngine engine, WorkflowPlan plan, ClaimWorkItem claim, CancellationToken cancellationToken)
    {
        // Gender is empty only when the national id could not be read. Checking Age == 0 instead would also skip
        // valid patients younger than one year.
        if (plan.RequiresPatientDemographics && string.IsNullOrEmpty(claim.Gender))
            return null;

        var outcome = new WorkItemOutcome();

        switch (plan.RejectionLevel)
        {
            case RejectionLevel.Activity:
                for (var i = 0; i < claim.Activities.Count; i++)
                {
                    var activityCode = claim.GetActivityCode(i);

                    // Only the rules that can possibly fail for this activity's code are evaluated.
                    await RunBatchesAsync(
                        engine, plan, plan.GetBatchesForActivity(activityCode), claim,
                        claim.Activities[i].ActivityId, activityCode, claim.GetActivityArgs(i), outcome, cancellationToken);
                }
                break;

            default: // RejectionLevel.Claim
                await RunBatchesAsync(
                    engine, plan, plan.Batches, claim, null, null, claim.GetClaimLevelArgs(), outcome, cancellationToken);
                break;
        }

        return outcome.IsEmpty ? null : outcome;
    }

    /// <param name="activityId">null for claim-level workflows (reported as ActivityId 0).</param>
    private async ValueTask RunBatchesAsync(
        RulesEngine engine,
        WorkflowPlan plan,
        IReadOnlyList<WorkflowBatch> batches,
        ClaimWorkItem claim,
        int? activityId,
        string? activityCode,
        RuleParameter[] args,
        WorkItemOutcome outcome,
        CancellationToken cancellationToken)
    {
        foreach (var batch in batches)
        {
            try
            {
                await batch.EnsureCompiledAsync(engine, args, cancellationToken);
                var results = await engine.ExecuteAllRulesAsync(batch.Name, args, cancellationToken);

                foreach (var result in results)
                {
                    if (!result.IsSuccess)
                    {
                        RecordViolation(plan, claim, activityId, activityCode, result, outcome);
                    }
                    else
                    {
                        if (!outcome.RulesEngineViolationLog.Any(x =>
                                                             x.ClaimNumber == claim.ClaimNumber &&
                                                             x.WorkFlowName == plan.Name &&
                                                             x.Activity == activityCode))
                        {
                            RecordSuccess(plan, claim, activityId, activityCode, result, outcome);
                        }
                    }
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

    private void RecordViolation(
        WorkflowPlan plan, ClaimWorkItem claim, int? activityId, string? activityCode,
        RuleResultTree result, WorkItemOutcome outcome)
    {
        var rule = result.Rule;
        var custom = rule as CustomEngineRule;   // the very instance registered in the snapshot: no reflection needed
        var rawMessage = result.ExceptionMessage;

        // Failing rules also carry their configured message here, so only the engine's own wording means "it threw".
        if (IsEngineError(rawMessage))
            LogRuleThrew(rule.RuleName, plan.Name, claim.ClaimNumber, activityId, rawMessage!);

         var violationType = custom?.ViolationType.ToString();
        var exceptionMessage = custom?.ExceptionMessage.ToString();

        var maxDailyQuantity = 0;
        if (result.Inputs.TryGetValue("ctx", out var ctx)
            && ctx is ActivityRuleContext { ActivityQuantity: { } quantity }
            && quantity == decimal.Truncate(quantity)
            && quantity >= int.MinValue && quantity <= int.MaxValue)
        {
            maxDailyQuantity = (int)quantity;
        }

        outcome.Failures.Add(new ClaimValidationResultDto
        {
            ActivityId = activityId ?? 0,
            ClaimNumber = claim.ClaimNumber,
            WorkFlowName = plan.Name,
            NormalizedWorkFlowName = plan.NormalizedName,
            Rule = rule.RuleName,
            ErrorMessage = exceptionMessage,
            ErrorType = violationType,
            MaxDailyQuantity = maxDailyQuantity
        });

        outcome.RulesEngineViolationLog.Add(RulesEngineExecutionLog.Create(
            plan.Name, plan.Id, rule.RuleName, custom?.RuleId ?? 0, violationType, exceptionMessage,
            claim.ClaimNumber, rule.Expression, activityCode!, true));
    }
    private void RecordSuccess(
        WorkflowPlan plan, ClaimWorkItem claim, int? activityId, string? activityCode,
        RuleResultTree result, WorkItemOutcome outcome)
    {
        var rule = result.Rule;
        var custom = rule as CustomEngineRule;   // the very instance registered in the snapshot: no reflection needed



        var maxDailyQuantity = 0;
        if (result.Inputs.TryGetValue("ctx", out var ctx)
            && ctx is ActivityRuleContext { ActivityQuantity: { } quantity }
            && quantity == decimal.Truncate(quantity)
            && quantity >= int.MinValue && quantity <= int.MaxValue)
        {
            maxDailyQuantity = (int)quantity;
        }



        outcome.RulesEngineViolationLog.Add(RulesEngineExecutionLog.Create(
            plan.Name, plan.Id, "", null, custom?.ViolationType.ToString(), result.ExceptionMessage,
            claim.ClaimNumber, rule.Expression, activityCode!, true));
    }

    private static bool IsEngineError(string? message)
        => message is not null && message.StartsWith("Error while ", StringComparison.Ordinal);

    private sealed class WorkItemOutcome
    {
        public List<ClaimValidationResultDto> Failures { get; } = new();
        public List<string> Errors { get; } = new();
        public List<RulesEngineExecutionLog> RulesEngineViolationLog { get; } = new();
        public bool IsEmpty => Failures.Count == 0 && Errors.Count == 0;
    }

    // =============================================================================================
    // Logging (source-generated: no boxing/formatting cost when the level is disabled)
    // =============================================================================================

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
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

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning,
        Message = "Extended validation completed in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}")]
    private partial void LogCompleted(long ms, int claimCount, int failed);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error,
        Message = "Extended validation completed with errors in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}, Errors: {Errors}. Details: {ErrorDetails}")]
    private partial void LogCompletedWithErrors(long ms, int claimCount, int failed, int errors, string errorDetails);


}
