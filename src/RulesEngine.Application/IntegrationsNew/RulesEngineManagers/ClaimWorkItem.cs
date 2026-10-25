using RulesEngine.Integrations;
using RulesEngine.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

/// <summary>
/// One claim, normalized once and shared (read-only) by every workflow evaluated against it.
/// Rule parameters that several workflows can reuse are created lazily and cached.
/// </summary>
internal sealed class ClaimWorkItem
{
    // Same bound as before: caching "all other activities" per code is O(n^2) memory for large claims.
    private const int MaxCachedOtherActivities = 64;
    private const string NullCodeKey = "\0<null>";
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
    private readonly ConcurrentDictionary<string, List<ClaimActivityRequestDto>>? _otherActivitiesByCode;
    private RuleParameter[]? _claimLevelArgs;   // benign race: both candidates are equivalent

    private ClaimWorkItem(ClaimValidationByCodeRequestExtended claim, int age, string gender, DateTime today)
    {
        _claim = claim;
        Age = age;
        Gender = gender;

        _diagnosisCodes = NormalizeCodes(claim.DiagnosisCodes);
        _activityCodes = NormalizeCodes(claim.ActivityCodes);
        Activities = claim.Activities ?? new List<ClaimActivityRequestDto>();
        _claimTags = claim.ClaimTags ?? new List<string>();

        _diagnosisCounts = CountCodes(_diagnosisCodes);
        _activityCodeCounts = CountCodes(_activityCodes);
        _quantitiesByCode = SumQuantitiesByCode(Activities);

        _activityCodeById = new Dictionary<int, string?>(Activities.Count);
        foreach (var activity in Activities)
            _activityCodeById.TryAdd(activity.ActivityId, activity.ActivityCode); // first one wins

        if (Activities.Count is > 0 and <= MaxCachedOtherActivities)
            _otherActivitiesByCode = new ConcurrentDictionary<string, List<ClaimActivityRequestDto>>(StringComparer.Ordinal);
        _specialitiesCode = claim.Claimspeccode;
        var activities = claim.Activities.ToList() ?? new List<ClaimActivityRequestDto>();

        _dtgCodeCounts = CountCodes(
                        activities
                            .SelectMany(a => (a.DtgCodes ?? new List<string>())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => x.Trim().ToUpperInvariant())
                                .Distinct())
                            .ToList());

         _genericsGroups = Activities
            .Where(a => !string.IsNullOrWhiteSpace(a.GenericCode))
            .Select(a => a.GenericCode!.Trim().ToUpperInvariant())
            .ToList();

        _diagnosisGroupCodes = claim.DiagnosisGroupCodes ?? new List<string>()
                  .Where(x => !string.IsNullOrWhiteSpace(x))
                  .Select(x => x.Trim().ToUpperInvariant())
                  .ToList();


       

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
            Activities = Activities,
            SpecialitiesCode=_specialitiesCode,
            DtgCodeCounts=_dtgCodeCounts
        })
    };

    public RuleParameter[] CreateAllActivityArgs(ClaimActivityRequestDto activity) => new[]
    {
        new RuleParameter("ctx", new AllActivityRuleContext
        {
            Diagnoses = _diagnosisCodes,
            DiagnosisCounts = _diagnosisCounts,
            PrimaryActivity = _activityCodeById[activity.ActivityId],
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
            Activities = GetOtherActivities(activity),
            SpecialitiesCode=_specialitiesCode,
            PrimaryGenericsGroup=activity.GenericCode,
            GenericsGroups=_genericsGroups,
            DiagnosisGroupCodes=_diagnosisGroupCodes,
            DtgCodeCounts=_dtgCodeCounts

        })
    };

    public RuleParameter[] CreateActivityArgs(ClaimActivityRequestDto activity) => new[]
    {
        new RuleParameter("ctx", new ActivityRuleContext
        {
            Diagnoses = _diagnosisCodes,
            DiagnosisCounts = _diagnosisCounts,
            PrimaryActivity =_activityCodeById[activity.ActivityId],
            MedicationCounts = _activityCodeCounts,
            NID = _claim.Patientnid,
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
            PatientAge = Age,
            PatientGender = Gender,
            Activities = new List<ClaimActivityRequestDto> { activity },
            ActivityQuantity = activity.Quantity,
            MedicationQuantities = _quantitiesByCode,
            SpecialitiesCode=_specialitiesCode,
            PrimaryGenericsGroup=activity.GenericCode,
            GenericsGroups=_genericsGroups,
            DiagnosisGroupCodes=_diagnosisGroupCodes,
            DtgCodeCounts=_dtgCodeCounts

        })
    };

    // ---- helpers ----------------------------------------------------------------------------------

    /// <summary>All activities whose code differs from the given activity's code.</summary>
    private List<ClaimActivityRequestDto> GetOtherActivities(ClaimActivityRequestDto activity)
    {
        if (_otherActivitiesByCode is null)
            return BuildOthers(Activities, activity.ActivityCode);

        return _otherActivitiesByCode.GetOrAdd(
            activity.ActivityCode ?? NullCodeKey,
            static (_, state) => BuildOthers(state.Activities, state.Code),
            (Activities, Code: activity.ActivityCode));
    }

    private static List<ClaimActivityRequestDto> BuildOthers(List<ClaimActivityRequestDto> all, string? code)
    {
        var others = new List<ClaimActivityRequestDto>(all.Count);
        foreach (var other in all)
        {
            if (!string.Equals(other.ActivityCode, code, StringComparison.Ordinal))
                others.Add(other);
        }
        return others;
    }

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
