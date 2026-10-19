using Microsoft.Extensions.Logging;
using RulesEngine.Common.Enums;
using RulesEngine.Lookups;
using RulesEngine.Models;
using RulesEngine.Repositories.WorkFlows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Integrations.RulesEngineManagers
{
    public class RulesValidationManager : IRulesValidationManager
    {
        private readonly ILogger<RulesEngineManager> _logger;
        private readonly IRepository<Diagnosis> _diagnosisRepository;
        private readonly IRulesEngineManager _rulesEngine;
        private readonly IWorkFlowRepository _workFlowRepository;

        public RulesValidationManager(ILogger<RulesEngineManager> logger, IRulesEngineManager rulesEngine, IWorkFlowRepository workFlowRepository, IRepository<Diagnosis> diagnosisRepository)
        {
            _logger = logger;
            _rulesEngine = rulesEngine;
            _workFlowRepository = workFlowRepository;
            _diagnosisRepository = diagnosisRepository;
        }

        public async Task<ClaimValidationResponse> SpecificValidateClaimsByCodesExtended(List<Guid>? workflowIds, List<string>? workflownames, List<ClaimValidationByCodeRequestExtended> claims)
        {
            if (claims == null || claims.Count == 0)
                return new ClaimValidationResponse { Success = "No Claims To Validate" };

            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting extended validation for {ClaimCount} claim(s). WorkflowIds: {WorkflowIds}, WorkflowNames: {WorkflowNames}",
                claims.Count,
                workflowIds != null ? string.Join(",", workflowIds) : "none",
                workflownames != null ? string.Join(",", workflownames) : "none");

            var rulesEngine = await _rulesEngine.GetRulesEngineAsync();

            var resolvedWorkflowIds = new HashSet<Guid>(workflowIds ?? new List<Guid>());

            var trimmedNames = (workflownames ?? new List<string>())
           .Where(n => !string.IsNullOrWhiteSpace(n))
           .Select(NormalizeWorkflowName)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToList();

            if (trimmedNames.Count > 0)
            {
                var allWorkflowNamePairs = (await _workFlowRepository.GetAllForRuleEngine())
                    .Select(wf => new { wf.Id, wf.Name })
                    .ToList();

                var workflowsFromNames = allWorkflowNamePairs
                    .Where(wf => trimmedNames.Contains(NormalizeWorkflowName(wf.Name)))
                    .ToList();

                var missingNames = trimmedNames
                    .Where(n => !workflowsFromNames.Any(
                        wf => string.Equals(NormalizeWorkflowName(wf.Name), n, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (missingNames.Count > 0)
                {
                    _logger.LogWarning("Workflow(s) not found: {MissingNames}", string.Join(", ", missingNames));
                    throw new UserFriendlyException($"Workflow(s) not found: {string.Join(", ", missingNames)}");
                }

                foreach (var workflow in workflowsFromNames)
                    resolvedWorkflowIds.Add(workflow.Id);
            }

            var finalWorkflowIds = resolvedWorkflowIds.Count > 0
                ? resolvedWorkflowIds.ToList()
                : (await _workFlowRepository.GetAllForRuleEngine()).Select(x => x.Id).ToList();

            var workflows = (await _workFlowRepository.GetAllForRuleEngine())
                .Where(wf => finalWorkflowIds.Contains(wf.Id))
                .Select(wf => new { wf.Id, wf.Name, HasRules = wf.Rules.Any(), RejectionLevel= wf.RejectionLevel })
                .ToList();

            var existingWorkflowIds = workflows.Select(w => w.Id).ToList();
            var missingWorkflowIds = finalWorkflowIds.Except(existingWorkflowIds).ToList();

            if (missingWorkflowIds.Count > 0)
            {
                _logger.LogError(
                    "Workflow(s) not found. Requested ids: {MissingWorkflowIds}",
                    string.Join(",", missingWorkflowIds));
                throw new UserFriendlyException(
                    $"Workflow(s) not found: {string.Join(",", missingWorkflowIds)}");
            }

            var workflowsWithoutRules = workflows.Where(w => !w.HasRules).ToList();

            if (workflowsWithoutRules.Count > 0)
            {
                _logger.LogWarning(
                    "Workflow(s) exist but have no rules attached, so they will not produce any results: {WorkflowNames}",
                    string.Join(",", workflowsWithoutRules.Select(w => w.Name)));
            }

            var workflowNames = workflows
                .Where(wf => wf.HasRules && !string.IsNullOrWhiteSpace(wf.Name))
                .Select(wf => wf.Name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var errors = new ConcurrentBag<string>();
            var failedResults = new ConcurrentBag<ClaimValidationResultDto>();

            static Dictionary<string, int> CountCodes(List<string> codes)
            {
                var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in codes)
                    dict[c] = dict.TryGetValue(c, out var n) ? n + 1 : 1;
                return dict;
            }

            static Dictionary<string, decimal> SumQuantitiesByCode(List<ClaimActivityRequestDto> activities)
            {
                var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var activity in activities)
                {
                    if (string.IsNullOrWhiteSpace(activity.ActivityCode))
                        continue;

                    var code = activity.ActivityCode.Trim().ToUpperInvariant();
                    var quantity = activity.Quantity ?? 0m;
                    dict[code] = dict.TryGetValue(code, out var existing) ? existing + quantity : quantity;
                }

                return dict;
            }

            static string? NormalizeWorkflowName(string? name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                var chars = name.Select(c => (!char.IsWhiteSpace(c) || c == ' ') ? c : ' ').ToArray();
                var collapsed = new string(chars);
                collapsed = Regex.Replace(collapsed, @"\s+", " ").Trim();
                return collapsed.ToUpperInvariant();
            }

            

            var workflowPlans = workflows.Select(x =>
            {
                var normalizedName = NormalizeWorkflowName(x.Name);
                var batchCount = _rulesEngine.GetBatchCount(x.Name);
                return new
                {
                    Name = x.Name,
                    NormalizedName = normalizedName,
                    RejectionLevel=x.RejectionLevel,
                    BatchNames = Enumerable.Range(0, Math.Max(0, batchCount))
                        .Select(index => $"{x.Name}__b{index}")
                        .ToArray()
                };
            }).ToArray();

            var normalizedClaims = claims.Select(c =>
            {
                var diagnosisCodes = (c.DiagnosisCodes ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToUpperInvariant())
                    .ToList();
                var activityCodes = (c.ActivityCodes ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToUpperInvariant())
                    .ToList();
                var activities = c.Activities ?? new List<ClaimActivityRequestDto>();

                return new
                {
                    Source = c,
                    DiagnosisCodes = diagnosisCodes,
                    ActivityCodes = activityCodes,
                    DiagnosisCounts = CountCodes(diagnosisCodes),
                    MedicationCounts = CountCodes(activityCodes),
                    MedicationQuantities = SumQuantitiesByCode(activities),
                    Activities = activities,
                    ActivityCodeById = activities
                        .GroupBy(activity => activity.ActivityId)
                        .ToDictionary(group => group.Key, group => group.First().ActivityCode),
                    ClaimTags = c.ClaimTags ?? new List<string>()
                };
            }).ToList();

            var allDiagnosisCodes = normalizedClaims
                .SelectMany(c => c.DiagnosisCodes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (allDiagnosisCodes.Count > 0)
            {
                var foundDiag = (await _diagnosisRepository.GetListAsync(d => allDiagnosisCodes.Contains(d.iqvia_code)))
                    .Select(d => d.iqvia_code)
                    .ToList();

                var foundSet = new HashSet<string>(
                    foundDiag.Select(x => x?.Trim().ToUpperInvariant() ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var nc in normalizedClaims)
                {
                    var invalid = nc.DiagnosisCodes.Where(c => !foundSet.Contains(c)).Distinct().ToList();
                    if (invalid.Count > 0)
                    {
                        _logger.LogWarning(
                            "Invalid diagnosis codes for claim (ClaimNumber: {ClaimNumber}): {InvalidCodes}",
                            nc.Source.ClaimNumber, string.Join(", ", invalid));
                        throw new UserFriendlyException(
                            $"Invalid diagnosis codes for claim (ClaimNumber: {nc.Source.ClaimNumber}): {string.Join(", ", invalid)}");
                    }
                }
            }

            await Parallel.ForAsync(0, normalizedClaims.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (claimIndex, claimCt) =>
            {
                var nc = normalizedClaims[claimIndex];
                var claim = nc.Source;

                var patientData = ExtractAgeAndGenderFromNationalId(claim.Patientnid);



                List<ClaimActivityRequestDto>[]? otherActivitiesByIndex = null;

                for (var workflowIndex = 0; workflowIndex < workflowPlans.Length; workflowIndex++)
                {
                    var workflow = workflowPlans[workflowIndex];
                    var workflowName = workflow.Name;

                    if (patientData.Age == 0 &&
                        (workflowName.Contains("age") || workflowName.Contains("gender")))
                        continue;

                    if (workflow.BatchNames.Length == 0)
                        continue;

                    if (workflow.RejectionLevel == RejectionLevel.AllActivity)
                    {
                        var activitiesOnClaim = nc.Activities;

                        if (activitiesOnClaim.Count == 0)
                            continue;

                        // Keep the reusable lists bounded: each one can contain almost every activity.
                        if (activitiesOnClaim.Count <= 64)
                        {
                            otherActivitiesByIndex ??= activitiesOnClaim
                                .Select(activity => activitiesOnClaim
                                    .Where(other => other.ActivityCode != activity.ActivityCode)
                                    .ToList())
                                .ToArray();
                        }

                        for (var activityIndex = 0; activityIndex < activitiesOnClaim.Count; activityIndex++)
                        {
                            var activity = activitiesOnClaim[activityIndex];
                            var otherActivities = otherActivitiesByIndex?[activityIndex]
                                ?? activitiesOnClaim
                                    .Where(other => other.ActivityCode != activity.ActivityCode)
                                    .ToList();
                            var perActivityCtx = new
                            {
                                Diagnoses = nc.DiagnosisCodes,
                                DiagnosisCounts = nc.DiagnosisCounts,
                                PrimaryActivity = nc.ActivityCodeById[activity.ActivityId],
                                ActivitiesQuantities = nc.MedicationCounts,
                                NationalId = claim.Patientnid,
                                ClaimedNet = claim.ClaimedNet,
                                EnteredNet = claim.EnteredNet,
                                ClaimedGross = claim.ClaimedGross,
                                EnteredGross = claim.EnteredGross,
                                ClaimStartDate = claim.ClaimStartDate,
                                ClaimEndDate = claim.ClaimEndDate,
                                BatchType = claim.BatchType,
                                BatchServiceMonth = claim.BatchServiceMonth,
                                BatchServiceYear = claim.BatchServiceYear,
                                ClaimTags = nc.ClaimTags,
                                Age = patientData.Age,
                                Gender = patientData.Gender,
                                Activities = otherActivities
                            };

                            var perActivityRuleParam = new RuleParameter("ctx", perActivityCtx);

                            for (var batchIndex = 0; batchIndex < workflow.BatchNames.Length; batchIndex++)
                            {
                                try
                                {
                                    var wfToRun = workflow.BatchNames[batchIndex];
                                    var results = await rulesEngine.ExecuteAllRulesAsync(wfToRun, perActivityRuleParam);

                                    foreach (var r in results.Where(x => !x.IsSuccess))
                                    {
                                        var ruleName = r.Rule.RuleName;
                                        if (!string.IsNullOrEmpty(r.ExceptionMessage))
                                        {
                                            _logger.LogError(
                                                "Rule {RuleName} in workflow {WorkflowName} threw during per-activity evaluation for claim {ClaimNumber}, activity {ActivityId}: {ExceptionMessage}",
                                                ruleName, workflowName, claim.ClaimNumber, activity.ActivityId, r.ExceptionMessage);
                                        }

                                        failedResults.Add(new ClaimValidationResultDto
                                        {
                                            ActivityId = activity.ActivityId,
                                            ClaimNumber = claim.ClaimNumber,
                                            WorkFlowName = workflowName,
                                            NormalizedWorkFlowName = workflow.NormalizedName,
                                        });

                                    }

                                }
                                catch (Exception ex)
                                {
                                    var msg = $"Workflow '{workflowName}' batch {batchIndex} for claim '{claim.ClaimNumber}' activity '{activity.ActivityId}' failed: {ex.Message}";
                                    _logger.LogError(ex, "Workflow {WorkflowName} batch {BatchIndex} for claim {ClaimNumber} activity {ActivityId} failed", workflowName, batchIndex, claim.ClaimNumber, activity.ActivityId);
                                    errors.Add(msg);
                                }
                            }
                        }
                    }
                    else if (workflow.RejectionLevel==RejectionLevel.Activity)
                    {
                        var activitiesOnClaim = nc.Activities;

                        if (activitiesOnClaim.Count == 0)
                            continue;

                        foreach (var activity in activitiesOnClaim)
                        {
                            var perActivityCtx = new
                            {
                                Diagnoses = nc.DiagnosisCodes,
                                DiagnosisCounts = nc.DiagnosisCounts,
                                CurrentMedications = new List<string> { nc.ActivityCodeById[activity.ActivityId] },
                                MedicationCounts = nc.MedicationCounts,
                                NID = claim.Patientnid,
                                ClaimedNet = claim.ClaimedNet,
                                EnteredNet = claim.EnteredNet,
                                ClaimedGross = claim.ClaimedGross,
                                EnteredGross = claim.EnteredGross,
                                ClaimStartDate = claim.ClaimStartDate,
                                ClaimEndDate = claim.ClaimEndDate,
                                BatchType = claim.BatchType,
                                BatchServiceMonth = claim.BatchServiceMonth,
                                BatchServiceYear = claim.BatchServiceYear,
                                ClaimTags = nc.ClaimTags,
                                PatientAge = patientData.Age,
                                PatientGender = patientData.Gender,
                                Activities = new List<ClaimActivityRequestDto> { activity },
                                ActivityQuantity = activity.Quantity,
                                MedicationQuantities = nc.MedicationQuantities
                            };
                            var perActivityRuleParam = new RuleParameter("ctx", perActivityCtx);

                            for (var batchIndex = 0; batchIndex < workflow.BatchNames.Length; batchIndex++)
                            {
                                try
                                {
                                    var wfToRun = workflow.BatchNames[batchIndex];
                                    var results = await rulesEngine.ExecuteAllRulesAsync(wfToRun, perActivityRuleParam);

                                    foreach (var r in results.Where(x => !x.IsSuccess))
                                    {
                                        var ruleName = r.Rule.RuleName;
                                        if (!string.IsNullOrEmpty(r.ExceptionMessage))
                                        {
                                            _logger.LogError(
                                                "Rule {RuleName} in workflow {WorkflowName} threw during per-activity evaluation for claim {ClaimNumber}, activity {ActivityId}: {ExceptionMessage}",
                                                ruleName, workflowName, claim.ClaimNumber, activity.ActivityId, r.ExceptionMessage);
                                        }

                                        failedResults.Add(new ClaimValidationResultDto
                                        {
                                            ActivityId = activity.ActivityId,
                                            ClaimNumber = claim.ClaimNumber,
                                            WorkFlowName = workflowName,
                                            NormalizedWorkFlowName = workflow.NormalizedName,
                                            Rule = ruleName,
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var msg = $"Workflow '{workflowName}' batch {batchIndex} for claim '{claim.ClaimNumber}' activity '{activity.ActivityId}' failed: {ex.Message}";
                                    _logger.LogError(ex, "Workflow {WorkflowName} batch {BatchIndex} for claim {ClaimNumber} activity {ActivityId} failed", workflowName, batchIndex, claim.ClaimNumber, activity.ActivityId);
                                    errors.Add(msg);
                                }
                            }
                        }
                    }
                    else
                    {
                        var claimLevelCtx = new
                        {

                            Diagnoses = nc.DiagnosisCodes,
                            DiagnosisCounts = nc.DiagnosisCounts,
                            PrimaryActivity = nc.ActivityCodes,
                            ActivitiesQuantities = nc.MedicationCounts,
                            NationalId = claim.Patientnid,
                            ClaimedNet = claim.ClaimedNet,
                            EnteredNet = claim.EnteredNet,
                            ClaimedGross = claim.ClaimedGross,
                            EnteredGross = claim.EnteredGross,
                            ClaimStartDate = claim.ClaimStartDate,
                            ClaimEndDate = claim.ClaimEndDate,
                            BatchType = claim.BatchType,
                            BatchServiceMonth = claim.BatchServiceMonth,
                            BatchServiceYear = claim.BatchServiceYear,
                            ClaimTags = nc.ClaimTags,
                            Age = patientData.Age,
                            Gender = patientData.Gender,
                            Activities = nc.Activities
                        };

                        var claimLevelRuleParam = new RuleParameter("ctx", claimLevelCtx);
                        for (var batchIndex = 0; batchIndex < workflow.BatchNames.Length; batchIndex++)
                        {
                            try
                            {
                                var wfToRun = workflow.BatchNames[batchIndex];
                                var results = await rulesEngine.ExecuteAllRulesAsync(wfToRun, claimLevelRuleParam);

                                foreach (var r in results.Where(x => !x.IsSuccess))
                                {
                                    var ruleName = r.Rule.RuleName;
                                    if (!string.IsNullOrEmpty(r.ExceptionMessage))
                                    {
                                        _logger.LogError(
                                            "Rule {RuleName} in workflow {WorkflowName} threw during evaluation for claim {ClaimNumber}: {ExceptionMessage}",
                                            ruleName, workflowName, claim.ClaimNumber, r.ExceptionMessage);
                                    }
                                    failedResults.Add(new ClaimValidationResultDto
                                    {
                                        ActivityId = 0,
                                        ClaimNumber = claim.ClaimNumber,
                                        WorkFlowName = workflowName,
                                        NormalizedWorkFlowName = workflow.NormalizedName,
                                        Rule = ruleName,
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                var msg = $"Workflow '{workflowName}' batch {batchIndex} for claim '{claim.ClaimNumber}' failed: {ex.Message}";
                                _logger.LogError(ex, "Workflow {WorkflowName} batch {BatchIndex} for claim {ClaimNumber} failed", workflowName, batchIndex, claim.ClaimNumber);
                                errors.Add(msg);
                            }
                        }
                    }
                }
            });

            sw.Stop();

            if (!errors.IsEmpty)
            {
                _logger.LogError(
                    "Extended validation completed with errors in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}, Errors: {Errors}. Details: {ErrorDetails}",
                    sw.ElapsedMilliseconds, claims.Count, failedResults.Count, errors.Count, string.Join(" | ", errors));
            }
            else
            {
                _logger.LogWarning(
                    "Extended validation completed in {Ms}ms for {ClaimCount} claim(s). Failed: {Failed}, Errors: {Errors}",
                    sw.ElapsedMilliseconds, claims.Count, failedResults.Count, errors.Count);
            }

            if (!errors.IsEmpty)
                throw new BusinessException("ValidationFailed").WithData("errors", errors);

            return failedResults.Any()
                ? new ClaimValidationResponse { Failed = failedResults.ToList() }
                : new ClaimValidationResponse { Success = "No Violation Found!" };
        }

        public static (int Age, string Gender) ExtractAgeAndGenderFromNationalId(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId) || nationalId.Length != 14)
                return (0, "");

            try
            {
                int centuryDigit = int.Parse(nationalId.Substring(0, 1));
                if (centuryDigit != 2 && centuryDigit != 3) return (0, "");

                int century = centuryDigit == 2 ? 1900 : 2000;
                int year = century + int.Parse(nationalId.Substring(1, 2));
                int month = int.Parse(nationalId.Substring(3, 2));
                int day = int.Parse(nationalId.Substring(5, 2));

                var birthDate = new DateTime(year, month, day);

                var today = DateTime.Today;
                int age = today.Year - birthDate.Year;
                if (birthDate.Date > today.AddYears(-age)) age--;

                int genderDigit = int.Parse(nationalId.Substring(12, 1));
                string gender = (genderDigit % 2 == 0) ? "Female" : "Male";

                return (age, gender);
            }
            catch
            {

                return (0, "");

            }
        }
        
    }
}
