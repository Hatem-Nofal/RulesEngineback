//using RulesEngine.Common.Enums;
//using RulesEngine.Models;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//namespace RulesEngine.Integrations.RulesEngineManagers;

///// <summary>
///// Everything the validation pipeline needs to know about a workflow, computed once per snapshot.
///// </summary>
//public sealed class WorkflowPlan
//{
//    public WorkflowPlan(long id, string name, string normalizedName, RejectionLevel rejectionLevel, IReadOnlyList<WorkflowBatch> batches)
//    {
//        Id = id;
//        Name = name;
//        NormalizedName = normalizedName;
//        RejectionLevel = rejectionLevel;
//        Batches = batches;

//        // Kept as-is from the original behaviour: a plain, case-sensitive substring match on the raw name.
//        RequiresPatientDemographics =
//            name.Contains("age", StringComparison.Ordinal) ||
//            name.Contains("gender", StringComparison.Ordinal);
//    }

//    public long Id { get; }
//    public string Name { get; }
//    public string NormalizedName { get; }
//    public RejectionLevel RejectionLevel { get; }

//    /// <summary>Engine batches (max 300 rules each). Empty when the workflow has no usable rules.</summary>
//    public IReadOnlyList<WorkflowBatch> Batches { get; }

//    public bool HasRules => Batches.Count > 0;

//    /// <summary>Workflow is skipped for claims whose national id yields no age/gender.</summary>
//    public bool RequiresPatientDemographics { get; }
//}

///// <summary>
///// One engine workflow ("&lt;name&gt;__b&lt;index&gt;"). RulesEngine 6.0.1 compiles lazily and without a lock, so
///// when many threads hit a cold batch each of them compiles all of its rules. <see cref="EnsureCompiledAsync"/>
///// lets exactly one thread compile while the others wait.
///// </summary>
//public sealed class WorkflowBatch
//{
//    private TaskCompletionSource? _compilation;

//    public WorkflowBatch(string name, int index)
//    {
//        Name = name;
//        Index = index;
//    }

//    public string Name { get; }
//    public int Index { get; }

//    public async ValueTask EnsureCompiledAsync(RulesEngine engine, RuleParameter[] args, CancellationToken cancellationToken)
//    {
//        var existing = Volatile.Read(ref _compilation);
//        if (existing is not null)
//        {
//            if (!existing.Task.IsCompleted)
//                await existing.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
//            return;
//        }

//        var mine = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
//        existing = Interlocked.CompareExchange(ref _compilation, mine, null);
//        if (existing is not null)
//        {
//            await existing.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
//            return;
//        }

//        try
//        {
//            // The first execution is what triggers compilation; its result is intentionally discarded.
//            await engine.ExecuteAllRulesAsync(Name, args, cancellationToken).ConfigureAwait(false);
//        }
//        catch
//        {
//            // Let the real execution report the problem, and let a later call retry the warm-up.
//            Interlocked.CompareExchange(ref _compilation, null, mine);
//        }
//        finally
//        {
//            mine.TrySetResult();
//        }
//    }
//}


using RulesEngine.Common.Enums;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RulesEngine.Integrations.RulesEngineManagers;

/// <summary>
/// Everything the validation pipeline needs to know about a workflow, computed once per snapshot.
/// </summary>
public sealed class WorkflowPlan
{
    private readonly IReadOnlyList<WorkflowBatch> _commonBatches;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<WorkflowBatch>> _batchesByActivityCode;

    /// <param name="batches">Every batch of the workflow (used when no activity filtering applies).</param>
    /// <param name="commonBatches">Batches holding rules that must run for every activity. Null means "all of <paramref name="batches"/>".</param>
    /// <param name="batchesByActivityCode">
    /// For each activity code, the batches (common + code-specific) to run for an activity with exactly that code.
    /// Null/empty disables filtering.
    /// </param>
    public WorkflowPlan(
        long id,
        string name,
        string normalizedName,
        RejectionLevel rejectionLevel,
        IReadOnlyList<WorkflowBatch> batches,
        IReadOnlyList<WorkflowBatch>? commonBatches = null,
        IReadOnlyDictionary<string, IReadOnlyList<WorkflowBatch>>? batchesByActivityCode = null,
        int anchoredRuleCount = 0,
        int commonRuleCount = 0)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
        RejectionLevel = rejectionLevel;
        Batches = batches;
        _commonBatches = commonBatches ?? batches;
        _batchesByActivityCode = batchesByActivityCode ?? new Dictionary<string, IReadOnlyList<WorkflowBatch>>(0);
        AnchoredRuleCount = anchoredRuleCount;
        CommonRuleCount = commonRuleCount;

        // Kept as-is from the original behaviour: a plain, case-sensitive substring match on the raw name.
        RequiresPatientDemographics =
            name.Contains("age", StringComparison.Ordinal) ||
            name.Contains("gender", StringComparison.Ordinal);
    }

    public long Id { get; }
    public string Name { get; }
    public string NormalizedName { get; }
    public RejectionLevel RejectionLevel { get; }

    /// <summary>Engine batches (max 300 rules each). Empty when the workflow has no usable rules.</summary>
    public IReadOnlyList<WorkflowBatch> Batches { get; }

    public bool HasRules => Batches.Count > 0;

    /// <summary>Rules that only apply to specific activity codes (skipped for every other activity).</summary>
    public int AnchoredRuleCount { get; }

    /// <summary>Rules evaluated for every activity.</summary>
    public int CommonRuleCount { get; }

    /// <summary>
    /// The batches that can possibly produce a violation for an activity with this code: the common rules plus the rules
    /// anchored on that exact code. Everything else is provably unable to fail and is not evaluated.
    /// </summary>
    public IReadOnlyList<WorkflowBatch> GetBatchesForActivity(string? activityCode)
        => activityCode is not null && _batchesByActivityCode.TryGetValue(activityCode, out var batches)
            ? batches
            : _commonBatches;

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
