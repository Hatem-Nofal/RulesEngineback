using RulesEngine.Entities.Lookups;
using RulesEngine.Integrations;
using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

// Strongly typed replacements for the three anonymous "ctx" objects. Property names and types are
// IDENTICAL to the old anonymous types, so every stored rule expression keeps working unchanged.

/// <summary>Members shared by every rule context.</summary>
public abstract class RuleContextBase
{
    public List<string> Diagnoses { get; init; } = new();
    public Dictionary<string, int> DiagnosisCounts { get; init; } = new();
    public decimal? ClaimedNet { get; init; }
    public decimal? EnteredNet { get; init; }
    public decimal? ClaimedGross { get; init; }
    public decimal? EnteredGross { get; init; }
    public DateTime? ClaimStartDate { get; init; }
    public DateTime? ClaimEndDate { get; init; }
    public string? BatchType { get; init; }
    public int? BatchServiceMonth { get; init; }
    public int? BatchServiceYear { get; init; }
    public List<string> ClaimTags { get; init; } = new();
    public string? SpecialitiesCode { get; set; }
    public Dictionary<string, int> DtgCodeCounts { get; set; }
    public string? NationalId { get; init; }
    public int Age { get; init; }
    public string Gender { get; init; } 

}

/// <summary>Context for workflows with RejectionLevel.Claim.</summary>
public sealed class ClaimRuleContext : RuleContextBase
{
     public Dictionary<string, int> ActivitiesQuantities { get; init; } = new();

    public List<string> Activities { get; init; } = new();
}

/// <summary>Context for workflows with RejectionLevel.AllActivity (Activities = all OTHER activity codes).</summary>
public sealed class AllActivityRuleContext : RuleContextBase
{
    public string? Activity { get; init; }
    public Dictionary<string, int> ActivitiesQuantities { get; init; } = new();
 
    public List<string> Activities { get; init; } = new();
    public string? GenericGroup { get; init; }
    public List<string> GenericsGroups { get; init; } = new();
    public List<string> DiagnosisGroupCodes { get; init; } = new();
}

/// <summary>Context for workflows with RejectionLevel.Activity (Activities = just the current one).</summary>
public sealed class ActivityRuleContext : RuleContextBase
{
    public string? Activity { get; init; }
    public string? GenericGroup { get; init; }
    public List<string> GenericsGroups { get; init; } = new();
    public List<string> DiagnosisGroupCodes { get; init; } = new();

    public Dictionary<string, int> MedicationCounts { get; init; } = new();
 
    public List<string> Activities { get; init; } = new();
    public decimal? ActivityQuantity { get; init; }
    public Dictionary<string, decimal> MedicationQuantities { get; init; } = new();

}

public static class RuleContextTypes
{
    /// <summary>
    /// Registered up-front in ReSettings.CustomTypes. RulesEngine otherwise swaps this array lazily during
    /// compilation, without locking, which is a data race when several threads compile at once.
    /// </summary>
    public static readonly Type[] All =
    {
        typeof(ClaimRuleContext),
        typeof(AllActivityRuleContext),
        typeof(ActivityRuleContext)
    };
}
