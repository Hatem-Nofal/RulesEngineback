using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RulesEngine.Integrations.RulesEngineManagers;

/// <summary>
/// Immutable view of the rules engine at one point in time. Workflow lookups are in-memory, so resolving
/// the requested workflows no longer touches the database.
/// </summary>
public sealed class RulesEngineSnapshot
{
    private static readonly IReadOnlyList<WorkflowPlan> NoPlans = Array.Empty<WorkflowPlan>();

    private readonly Dictionary<Guid, WorkflowPlan> _byId = new();
    private readonly Dictionary<string, WorkflowPlan> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<WorkflowPlan>> _byNormalizedName = new(StringComparer.OrdinalIgnoreCase);

    public RulesEngineSnapshot(RulesEngine engine, IReadOnlyList<WorkflowPlan> workflows)
    {
        Engine = engine;
        Workflows = workflows;

        foreach (var plan in workflows)
        {
            _byId[plan.Id] = plan;
            _byName.TryAdd(plan.Name, plan);

            if (!_byNormalizedName.TryGetValue(plan.NormalizedName, out var list))
                _byNormalizedName[plan.NormalizedName] = list = new List<WorkflowPlan>(1);
            list.Add(plan);
        }
    }

    public RulesEngine Engine { get; }

    /// <summary>All active workflows, including those without rules.</summary>
    public IReadOnlyList<WorkflowPlan> Workflows { get; }

    public bool TryGetById(Guid id, [NotNullWhen(true)] out WorkflowPlan? plan)
        => _byId.TryGetValue(id, out plan);

    public WorkflowPlan? FindByName(string name)
        => _byName.GetValueOrDefault(name);

    /// <summary>Several workflows can share a normalized name (names differing only in spacing/case).</summary>
    public IReadOnlyList<WorkflowPlan> FindByNormalizedName(string normalizedName)
        => _byNormalizedName.TryGetValue(normalizedName, out var list) ? list : NoPlans;
}
