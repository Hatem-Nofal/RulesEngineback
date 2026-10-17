using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RulesEngine.Common.Helpers;
using RulesEngine.Integrations;
using RulesEngine.Models;
using RulesEngine.Repositories.WorkFlows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace RulesEngine.Integrations.RulesEngineManagers
{
    public class RulesEngineManager : IRulesEngineManager, ISingletonDependency
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RulesEngineManager> _logger;

        private readonly SemaphoreSlim _reloadLock = new(1, 1);

        private volatile RulesEngine _engine;

        private readonly ConcurrentDictionary<string, int> _workflowBatchCounts =
            new(StringComparer.OrdinalIgnoreCase);

        private const int BatchSize = 300;

        public RulesEngineManager(IServiceScopeFactory scopeFactory, ILogger<RulesEngineManager> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public int GetBatchCount(string baseWorkflowName)
            => _workflowBatchCounts.TryGetValue(baseWorkflowName, out var c) ? c : 0;

        public async Task<RulesEngine> GetRulesEngineAsync()
        {
            var engine = _engine;
            if (engine != null)
                return engine;

            await _reloadLock.WaitAsync();
            try
            {
                // Another request may have initialized the engine while we waited.
                engine = _engine;
                if (engine == null)
                {
                    await BuildAndPublishWorkflowsAsync();
                    engine = _engine;
                }

                return engine;
            }
            finally
            {
                _reloadLock.Release();
            }
        }

        public async Task ReloadWorkflowsAsync()
        {
            await _reloadLock.WaitAsync();
            try
            {
                await BuildAndPublishWorkflowsAsync();
            }
            finally
            {
                _reloadLock.Release();
            }
        }

        private async Task BuildAndPublishWorkflowsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IWorkFlowRepository>();


                var workflowsFromDb = await repo.GetAllForRuleEngine();

                var valid = workflowsFromDb
                    .Where(wf =>
                        wf != null &&
                        !string.IsNullOrWhiteSpace(wf.Name) &&
                        wf.Rules != null &&
                        wf.Rules.Count > 0)
                    .ToList();

                var engineWorkflows = new List<Workflow>(Math.Max(16, valid.Count * 2));
                var newBatchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var wf in valid)
                {
                    var mappedRules = wf.Rules
                        .Where(r =>
                            r != null &&
                            !string.IsNullOrWhiteSpace(r.Name) &&
                            !string.IsNullOrWhiteSpace(r.Expression))
                        .Select(r => new Rule
                        {
                            RuleName = r.Name,
                            Enabled = r.IsActive,
                            SuccessEvent = r.SuccessEvent,
                            ErrorMessage = r.ErrorMessage,
                            Expression = r.Expression
                        })
                        .ToList();

                    if (mappedRules.Count == 0)
                    {
                        newBatchCounts[wf.Name] = 0;
                        continue;
                    }

                    //if (mappedRules.Count <= BatchSize)
                    //{
                    //    engineWorkflows.Add(new Workflow
                    //    {
                    //        WorkflowName = wf.Name,
                    //        Rules = mappedRules
                    //    });

                    //    newBatchCounts[wf.Name] = 1;
                    //    continue;
                    //}

                    int batchIndex = 0;

                    for (int start = 0; start < mappedRules.Count; start += BatchSize)
                    {
                        int count = Math.Min(BatchSize, mappedRules.Count - start);

                        var batchRules = new List<Rule>(count);
                        for (int i = 0; i < count; i++)
                            batchRules.Add(mappedRules[start + i]);

                        engineWorkflows.Add(new Workflow
                        {
                            WorkflowName = $"{wf.Name}__b{batchIndex}",
                            Rules = batchRules
                        });

                        batchIndex++;
                    }

                    newBatchCounts[wf.Name] = batchIndex;
                }

                var newEngine = new RulesEngine(engineWorkflows.ToArray(), new ReSettings
                {
                    // Batch code collects matches; no automatic action execution.
                    AutoExecuteActions = false,

                    // Benchmark this against true on your real expressions.
                    UseFastExpressionCompiler = false,

                    // Useful when compiling a large workflow.
                    EnableParallelRuleCompilation = true
                });

                _workflowBatchCounts.Clear();
                foreach (var kv in newBatchCounts)
                    _workflowBatchCounts[kv.Key] = kv.Value;

                _engine = newEngine;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load workflows into the rules engine");
                throw;
            }
        }

    }
}
