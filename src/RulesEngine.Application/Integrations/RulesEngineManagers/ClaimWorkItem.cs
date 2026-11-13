

using RulesEngine.Common.Enums;
using RulesEngine.Integrations;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace RulesEngine.Integrations.RulesEngineManagers;

/// <summary>
/// One claim, normalized once and shared (read-only) by every workflow evaluated against it.
/// Rule parameters that several workflows can reuse are created lazily and cached.
/// </summary>
internal sealed class ClaimWorkItem
{
    private readonly string? _specialitiesCode;
    private readonly string? _primaryGenericsGroup;

    private readonly ClaimValidationByCodeRequestExtended _claim;
    private readonly List<string> _diagnosisCodes;
    private readonly List<string> _activityCodes;
    private readonly Dictionary<string, int> _diagnosisCounts;
    private readonly Dictionary<string, int> _activityCodeCounts;
    private readonly Dictionary<string, decimal> _quantitiesByCode;
    private readonly Dictionary<int, string?> _activityCodeById;
    private readonly List<string> _claimTags;
    private readonly Dictionary<string, int> _dtgCodeCounts;
    private readonly List<string> _genericsGroups;
    private readonly List<string> _diagnosisGroupCodes;
    private readonly RuleParameter[]?[] _activityArgs;   // one slot per activity, shared by every workflow
    private RuleParameter[]? _claimLevelArgs;            // benign race: both candidates are equivalent

    private ClaimWorkItem(ClaimValidationByCodeRequestExtended claim, int age, string gender, DateTime today)
    {
        _claim = claim;
        Age = age;
        Gender = gender;

        Activities = claim.Activities ?? new List<ClaimActivityRequestDto>();
        _activityArgs = new RuleParameter[Activities.Count][];

        _diagnosisCodes = NormalizeCodes(claim.DiagnosisCodes);
        _activityCodes = NormalizeCodes(Activities.Select(x => x.ActivityCode).ToList());
        _claimTags = claim.ClaimTags ?? new List<string>();

        _diagnosisCounts = CountCodes(_diagnosisCodes);
        _activityCodeCounts = CountCodes(_activityCodes);
        _quantitiesByCode = SumQuantitiesByCode(Activities);

        _activityCodeById = new Dictionary<int, string?>(Activities.Count);
        foreach (var activity in Activities)
            _activityCodeById.TryAdd(activity.ActivityId, activity.ActivityCode); // first one wins

        _specialitiesCode = claim.Claimspeccode;

        _dtgCodeCounts = CountCodes(
                        Activities
                            .SelectMany(a => (a.DtgCodes ?? new List<string>())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => x.Trim().ToUpperInvariant())
                                .Distinct())
                            .ToList());

        _genericsGroups = Activities
           .Where(a => !string.IsNullOrWhiteSpace(a.GenericCode))
           .Select(a => a.GenericCode!.Trim().ToUpperInvariant())
           .ToList();

        // Normalized like every other code list. (The previous "a ?? b.Where(..)" only normalized the empty fallback.)
        _diagnosisGroupCodes = NormalizeCodes(claim.DiagnosisGroupCodes);

    }

    public string? ClaimNumber => _claim.ClaimNumber;
    public IReadOnlyList<string> DiagnosisCodes => _diagnosisCodes;
    public List<ClaimActivityRequestDto> Activities { get; }
    public int Age { get; }
    public string Gender { get; }

    public static ClaimWorkItem Create(ClaimValidationByCodeRequestExtended claim, DateTime today)
    {
        return new ClaimWorkItem(claim, claim.Age, claim.Gender, today);
    }

    // ---- rule parameters --------------------------------------------------------------------------

    public RuleParameter[] GetClaimLevelArgs() => _claimLevelArgs ??= new[]
    {
        new RuleParameter("ctx", new ClaimRuleContext
        {
            Diagnoses = _diagnosisCodes,
            DiagnosisCounts = _diagnosisCounts,
            ActivitiesQuantities = _activityCodeCounts,
            NationalId = _claim.Patientnid,
            ClaimedNet = _claim.ClaimedNet,
            EnteredNet = _claim.EnteredNet,
            ClaimedGross = _claim.ClaimedGross,
            EnteredGross = _claim.EnteredGross,
            ClaimStartDate = _claim.ClaimStartDate,
            ClaimEndDate = _claim.ClaimEndDate,
            BatchType = _claim.BatchType,
            BatchServiceMonth = _claim.BatchServiceMonth,
            BatchServiceYear = _claim.BatchServiceYear,
            ClaimTags = _claimTags,
            Age = Age,
            Gender = Gender,
            Activities = Activities.Select(x=>x.ActivityCode).ToList(),
            SpecialitiesCode=_specialitiesCode,
            DtgCodeCounts=_dtgCodeCounts
        })
    };


    /// <summary>
    /// The rule parameters for the activity at <paramref name="index"/>, built once and reused by every workflow that
    /// evaluates this claim (the context is read-only, so sharing it across threads is safe).
    /// </summary>
    public RuleParameter[] GetActivityArgs(int index)
        => _activityArgs[index] ??= CreateActivityArgs(Activities[index]);

    /// <summary>The code rules see as <c>ctx.Activity</c> for the activity at <paramref name="index"/>.</summary>
    public string? GetActivityCode(int index) => _activityCodeById[Activities[index].ActivityId];

    private RuleParameter[] CreateActivityArgs(ClaimActivityRequestDto activity) => new[]
    {
        new RuleParameter("ctx", new ActivityRuleContext
        {
            Diagnoses = _diagnosisCodes,
            DiagnosisCounts = _diagnosisCounts,
            Activity =_activityCodeById[activity.ActivityId],
            MedicationCounts = _activityCodeCounts,
            NationalId = _claim.Patientnid,
            ClaimedNet = _claim.ClaimedNet,
            EnteredNet = _claim.EnteredNet,
            ClaimedGross = _claim.ClaimedGross,
            EnteredGross = _claim.EnteredGross,
            ClaimStartDate = _claim.ClaimStartDate,
            ClaimEndDate = _claim.ClaimEndDate,
            BatchType = _claim.BatchType,
            BatchServiceMonth = _claim.BatchServiceMonth,
            BatchServiceYear = _claim.BatchServiceYear,
            ClaimTags = _claimTags,
            Age = Age,
            Gender = Gender,
            Activities = _activityCodes,
            ActivityQuantity = activity.Quantity,
            MedicationQuantities = _quantitiesByCode,
            SpecialitiesCode=_specialitiesCode,
            GenericGroup=activity.GenericCode,
            GenericsGroups=_genericsGroups,
            DiagnosisGroupCodes=_diagnosisGroupCodes,
            DtgCodeCounts=_dtgCodeCounts

        })
    };

    /// <summary>Empty contexts of the right type, used to compile a workflow's rules ahead of the first real request.</summary>
    public static RuleParameter[] CreateWarmupArgs(RejectionLevel level) => level == RejectionLevel.Activity
        ? new[] { new RuleParameter("ctx", new ActivityRuleContext { Gender = string.Empty }) }
        : new[] { new RuleParameter("ctx", new ClaimRuleContext { Gender = string.Empty }) };

    // ---- helpers ----------------------------------------------------------------------------------

    private static List<string> NormalizeCodes(List<string>? codes)
    {
        var result = new List<string>(codes?.Count ?? 0);
        if (codes is null) return result;

        foreach (var code in codes)
        {
            if (!string.IsNullOrWhiteSpace(code))
                result.Add(code.Trim().ToUpperInvariant());
        }
        return result;
    }

    private static Dictionary<string, int> CountCodes(List<string> codes)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
            CollectionsMarshal.GetValueRefOrAddDefault(counts, code, out _)++;
        return counts;
    }

    private static Dictionary<string, decimal> SumQuantitiesByCode(List<ClaimActivityRequestDto> activities)
    {
        var sums = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var activity in activities)
        {
            if (string.IsNullOrWhiteSpace(activity.ActivityCode))
                continue;

            var code = activity.ActivityCode.Trim().ToUpperInvariant();
            CollectionsMarshal.GetValueRefOrAddDefault(sums, code, out _) += activity.Quantity ?? 0m;
        }
        return sums;
    }
}

