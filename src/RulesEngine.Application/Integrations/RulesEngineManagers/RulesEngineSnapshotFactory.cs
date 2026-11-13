using RulesEngine.Common.Dtos;
using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.HelperFunctions;
using RulesEngine.IntegrationsNew.RulesEngineManagers;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using EngineRule = RulesEngine.Models.Rule;
using EngineWorkflow = RulesEngine.Models.Workflow;

namespace RulesEngine.Integrations.RulesEngineManagers;

/// <summary>Pure function: database workflows in, ready-to-use snapshot out.</summary>
internal static class RulesEngineSnapshotFactory
{
    public const int BatchSize = 300;

    public static RulesEngineSnapshot Create(IEnumerable<WorkFlow> workflows)
    {
        var plans = new List<WorkflowPlan>();
        var engineWorkflows = new List<EngineWorkflow>();

        foreach (var wf in workflows)
        {
            if (wf is null || string.IsNullOrWhiteSpace(wf.Name))
                continue;

            plans.Add(BuildPlan(wf, engineWorkflows));
        }

        var engine = new RulesEngine(engineWorkflows.ToArray(), CreateSettings(engineWorkflows.Count));
        return new RulesEngineSnapshot(engine, plans);
    }

    /// <summary>
    /// Splits a workflow's rules into engine batches. For activity-level workflows, rules that can only fire for specific
    /// activity codes (see <see cref="RuleAnchorAnalyzer"/>) go into per-code batches, so validating an activity runs
    /// "common rules + rules for its code" instead of every rule of the workflow.
    /// </summary>
    private static WorkflowPlan BuildPlan(WorkFlow wf, List<EngineWorkflow> engineWorkflows)
    {
        var rules = MapRules(wf);
        var allBatches = new List<WorkflowBatch>();
        var filterByActivity = wf.RejectionLevel == RejectionLevel.Activity;

        var common = new List<EngineRule>(rules.Count);
        var byCode = new Dictionary<string, List<EngineRule>>(StringComparer.Ordinal);
        var anchoredRules = 0;

        foreach (var rule in rules)
        {
            var anchor = filterByActivity ? RuleAnchorAnalyzer.TryGetActivityAnchor(rule.Expression) : null;
            if (anchor is null)
            {
                common.Add(rule);
                continue;
            }

            anchoredRules++;
            foreach (var code in anchor)
            {
                if (!byCode.TryGetValue(code, out var bucket))
                    byCode[code] = bucket = new List<EngineRule>();

                // One instance per engine workflow: the engine must not share Rule objects between workflows.
                bucket.Add(Clone(rule));
            }
        }

        var commonBatches = AddBatches(wf.Name, common, allBatches, engineWorkflows);

        IReadOnlyDictionary<string, IReadOnlyList<WorkflowBatch>>? perCode = null;
        if (byCode.Count > 0)
        {
            var map = new Dictionary<string, IReadOnlyList<WorkflowBatch>>(byCode.Count, StringComparer.Ordinal);
            var bucketNumber = 0;
            foreach (var (code, bucketRules) in byCode)
            {
                var own = AddBatches($"{wf.Name}__a{bucketNumber++}", bucketRules, allBatches, engineWorkflows);
                var combined = new List<WorkflowBatch>(commonBatches.Count + own.Count);
                combined.AddRange(commonBatches);
                combined.AddRange(own);
                map[code] = combined;
            }
            perCode = map;
        }

        return new WorkflowPlan(
            wf.Id,
            wf.Name,
            WorkflowNameNormalizer.Normalize(wf.Name)!,
            wf.RejectionLevel,
            allBatches,
            commonBatches,
            perCode,
            anchoredRuleCount: anchoredRules,
            commonRuleCount: common.Count);
    }

    private static List<WorkflowBatch> AddBatches(
        string namePrefix,
        List<EngineRule> rules,
        List<WorkflowBatch> allBatches,
        List<EngineWorkflow> engineWorkflows)
    {
        var created = new List<WorkflowBatch>();
        for (var start = 0; start < rules.Count; start += BatchSize)
        {
            var batchName = $"{namePrefix}__b{allBatches.Count}";
            engineWorkflows.Add(new EngineWorkflow
            {
                WorkflowName = batchName,
                Rules = rules.GetRange(start, Math.Min(BatchSize, rules.Count - start))
            });

            var batch = new WorkflowBatch(batchName, allBatches.Count);
            allBatches.Add(batch);
            created.Add(batch);
        }
        return created;
    }

    private static List<CustomEngineRule> MapRules(WorkFlow wf)
    {
        var mapped = new List<CustomEngineRule>(wf.Rules?.Count ?? 0);
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
                ViolationType = r.ViolationType,
                RuleId = r.Id,
                ExceptionMessage = r.ExceptionMessage

            });
        }
        return mapped;
    }

    private static CustomEngineRule Clone(CustomEngineRule r) => new()
    {
        RuleName = r.RuleName,
        Enabled = r.Enabled,
        ErrorMessage = r.ErrorMessage,
        Expression = r.Expression,
        ViolationType = r.ViolationType,
        RuleId = r.RuleId,
        ExceptionMessage = r.ExceptionMessage
    };

    private static ReSettings CreateSettings(int batchCount) => new()
    {
        // We only read results; skip the per-result action pass.
        AutoExecuteActions = false,

        // Parallel compilation is silently ignored when this is true (RulesEngine 6.0.1), so the two are
        // mutually exclusive. Compile now happens once per process, so benchmarking 'true' here is worthwhile.
        UseFastExpressionCompiler = false,
        EnableParallelRuleCompilation = true,

        // See RuleContextTypes.All: avoids RulesEngine mutating CustomTypes while compiling concurrently,
        // and registers the LargeRuleLists helper that multi-value conditions call.
        CustomTypes = RuleContextTypes.All,

        // Default is 1000 entries with FIFO eviction; an evicted batch is recompiled. Leave generous headroom.
        CacheConfig = new MemCacheConfig { SizeLimit = Math.Max(1000, batchCount * 4) }
    };
}
