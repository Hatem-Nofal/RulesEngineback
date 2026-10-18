using RulesEngine.Common.Enums;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

/// <summary>
/// Everything the validation pipeline needs to know about a workflow, computed once per snapshot.
/// </summary>
public sealed class WorkflowPlan
{
    public WorkflowPlan(Guid id, string name, string normalizedName, RejectionLevel rejectionLevel, IReadOnlyList<WorkflowBatch> batches)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
        RejectionLevel = rejectionLevel;
        Batches = batches;

        // Kept as-is from the original behaviour: a plain, case-sensitive substring match on the raw name.
        RequiresPatientDemographics =
            name.Contains("age", StringComparison.Ordinal) ||
            name.Contains("gender", StringComparison.Ordinal);
    }

    public Guid Id { get; }
    public string Name { get; }
    public string NormalizedName { get; }
    public RejectionLevel RejectionLevel { get; }

    /// <summary>Engine batches (max 300 rules each). Empty when the workflow has no usable rules.</summary>
    public IReadOnlyList<WorkflowBatch> Batches { get; }

    public bool HasRules => Batches.Count > 0;

    /// <summary>Workflow is skipped for claims whose national id yields no age/gender.</summary>
    public bool RequiresPatientDemographics { get; }
}

/// <summary>
/// One engine workflow ("&lt;name&gt;__b&lt;index&gt;"). RulesEngine 6.0.1 compiles lazily and without a lock, so
/// when many threads hit a cold batch each of them compiles all of its rules. <see cref="EnsureCompiledAsync"/>
/// lets exactly one thread compile while the others wait.
/// </summary>
public sealed class WorkflowBatch
{
    private TaskCompletionSource? _compilation;

    public WorkflowBatch(string name, int index)
    {
        Name = name;
        Index = index;
    }

    public string Name { get; }
    public int Index { get; }

    public async ValueTask EnsureCompiledAsync(RulesEngine engine, RuleParameter[] args, CancellationToken cancellationToken)
    {
        var existing = Volatile.Read(ref _compilation);
        if (existing is not null)
        {
            if (!existing.Task.IsCompleted)
                await existing.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var mine = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        existing = Interlocked.CompareExchange(ref _compilation, mine, null);
        if (existing is not null)
        {
            await existing.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            // The first execution is what triggers compilation; its result is intentionally discarded.
            await engine.ExecuteAllRulesAsync(Name, args, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Let the real execution report the problem, and let a later call retry the warm-up.
            Interlocked.CompareExchange(ref _compilation, null, mine);
        }
        finally
        {
            mine.TrySetResult();
        }
    }
}
