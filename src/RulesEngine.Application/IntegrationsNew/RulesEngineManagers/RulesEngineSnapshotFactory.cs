using RulesEngine.Common.Dtos;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.HelperFunctions;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using EngineRule = RulesEngine.Models.Rule;
using EngineWorkflow = RulesEngine.Models.Workflow;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

/// <summary>Pure function: database workflows in, ready-to-use snapshot out.</summary>
internal static class RulesEngineSnapshotFactory
{
    public const int BatchSize = 300;

    public static RulesEngineSnapshot Create(IEnumerable<Entities.WorkFlows.WorkFlow> workflows)
    {
        var plans = new List<WorkflowPlan>();
        var engineWorkflows = new List<EngineWorkflow>();

        foreach (var wf in workflows)
        {
            if (wf is null || string.IsNullOrWhiteSpace(wf.Name))
                continue;

            var batches = new List<WorkflowBatch>();
            var rules = MapRules(wf);

            for (var start = 0; start < rules.Count; start += BatchSize)
            {
                var batchName = $"{wf.Name}__b{batches.Count}";
                engineWorkflows.Add(new EngineWorkflow
                {
                    WorkflowName = batchName,
                    Rules = rules.GetRange(start, Math.Min(BatchSize, rules.Count - start))
                });
                batches.Add(new WorkflowBatch(batchName, batches.Count));
            }

            plans.Add(new WorkflowPlan(
                wf.Id,
                wf.Name,
                WorkflowNameNormalizer.Normalize(wf.Name)!,
                wf.RejectionLevel,
                batches));
        }

        var engine = new RulesEngine(engineWorkflows.ToArray(), CreateSettings(engineWorkflows.Count));
        return new RulesEngineSnapshot(engine, plans);
    }

    private static List<EngineRule> MapRules(WorkFlow wf)
    {
        var mapped = new List<EngineRule>(wf.Rules?.Count ?? 0);
        if (wf.Rules is null) return mapped;

        foreach (var r in wf.Rules)
        {
            if (r is null || string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Expression))
                continue;

            mapped.Add(new CustomEngineRule
            {
                RuleName = r.Name,
                Enabled = r.IsActive,
                ErrorMessage = r.ViolationMessage,
                Expression = r.Expression,
                ViolationType=r.ViolationType
             });
        }
        return mapped;
    }

    private static ReSettings CreateSettings(int batchCount) => new()
    {
        // We only read results; skip the per-result action pass.
        AutoExecuteActions = false,

        // Parallel compilation is silently ignored when this is true (RulesEngine 6.0.1), so the two are
        // mutually exclusive. Compile now happens once per process, so benchmarking 'true' here is worthwhile.
        UseFastExpressionCompiler = false,
        EnableParallelRuleCompilation = true,

        // See RuleContextTypes.All: avoids RulesEngine mutating CustomTypes while compiling concurrently.
        CustomTypes = RuleContextTypes.All,

        // Default is 1000 entries with FIFO eviction; an evicted batch is recompiled. Leave generous headroom.
        CacheConfig = new MemCacheConfig { SizeLimit = Math.Max(1000, batchCount * 4) }
    };
}
