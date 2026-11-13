using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RulesEngine.Common.Enums;
using RulesEngine.Repositories.WorkFlows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Linq;
using Volo.Abp.Uow;

namespace RulesEngine.Integrations.RulesEngineManagers;

/// <summary>
/// Owns the compiled rules engine. Singleton: the engine is built once and reused, and is replaced
/// atomically when workflows/rules change (explicit reload, or detected by a cheap periodic fingerprint check).
/// </summary>
public class RulesEngineManager : IRulesEngineManager, ISingletonDependency
{
    /// <summary>How often a request may pay for the (two small aggregate queries) staleness check.</summary>
    private const long FreshnessCheckIntervalMs = 10_000;

    private const string ReloadScheduledKey = "RulesEngine.ReloadScheduled";

    private sealed record LoadedState(RulesEngineSnapshot Snapshot, RulesFingerprint Fingerprint);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<RulesEngineManager> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private volatile LoadedState? _state;
    private long _lastFreshnessCheck;

    public RulesEngineManager(
        IServiceScopeFactory scopeFactory,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<RulesEngineManager> logger)
    {
        _scopeFactory = scopeFactory;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public int GetBatchCount(string baseWorkflowName)
        => _state?.Snapshot.FindByName(baseWorkflowName)?.Batches.Count ?? 0;

    public async Task<RulesEngine> GetRulesEngineAsync()
        => (await GetSnapshotAsync().ConfigureAwait(false)).Engine;

    public async Task<RulesEngineSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var state = _state;
        if (state is null)
            return await LoadFirstSnapshotAsync(cancellationToken).ConfigureAwait(false);

        // The staleness check (and a rebuild + warm-up when something changed) runs in the background: requests keep
        // using the current, already-compiled snapshot instead of waiting for it.
        if (TryClaimFreshnessCheck())
            _ = Task.Run(() => RefreshIfChangedAsync(CancellationToken.None));

        return (_state ?? state).Snapshot;
    }

    public async Task ReloadWorkflowsAsync()
    {
        await _reloadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await BuildAndPublishAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public Task ReloadAfterCommitAsync()
    {
        var uow = _unitOfWorkManager.Current;
        if (uow is null)
            return ReloadQuietlyAsync();

        // Bulk operations call this once per row; reload only once per unit of work.
        if (uow.Items.ContainsKey(ReloadScheduledKey))
            return Task.CompletedTask;

        uow.Items[ReloadScheduledKey] = true;
        uow.OnCompleted(ReloadQuietlyAsync);
        return Task.CompletedTask;
    }

    // ---- internals --------------------------------------------------------------------------------

    private async Task<RulesEngineSnapshot> LoadFirstSnapshotAsync(CancellationToken cancellationToken)
    {
        await _reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Another request may have loaded it while we waited.
            if (_state is null)
                await BuildAndPublishAsync(cancellationToken).ConfigureAwait(false);

            return _state!.Snapshot;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    /// <summary>True for exactly one caller per interval: the one that wins the compare-and-swap on the timestamp.</summary>
    private bool TryClaimFreshnessCheck()
    {
        var now = Environment.TickCount64;
        var last = Volatile.Read(ref _lastFreshnessCheck);
        return now - last >= FreshnessCheckIntervalMs
               && Interlocked.CompareExchange(ref _lastFreshnessCheck, now, last) == last;
    }

    /// <summary>Never throws (other than cancellation): on any failure the current snapshot keeps serving.</summary>
    private async Task RefreshIfChangedAsync(CancellationToken cancellationToken)
    {
        // Someone else is already checking/reloading: just keep using the current snapshot.
        if (!await _reloadLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkFlowRepository>();
            var current = await repository.GetRulesFingerprintAsync(cancellationToken).ConfigureAwait(false);

            if (_state is null || current != _state.Fingerprint)
            {
                _logger.LogInformation("Workflows/rules changed in the database; rebuilding the rules engine");
                await BuildAndPublishAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not check rules freshness; continuing with the current engine");
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private async Task ReloadQuietlyAsync()
    {
        try
        {
            await ReloadWorkflowsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The periodic fingerprint check will retry; a failed reload must not fail the caller's request.
            _logger.LogError(ex, "Reloading the rules engine after a change failed");
        }
    }

    /// <summary>Compiles every batch of the new snapshot in parallel (each batch compiles exactly once).</summary>
    private static Task WarmUpAsync(RulesEngineSnapshot snapshot, CancellationToken cancellationToken)
    {
        var claimArgs = ClaimWorkItem.CreateWarmupArgs(RejectionLevel.Claim);
        var activityArgs = ClaimWorkItem.CreateWarmupArgs(RejectionLevel.Activity);

        var work = snapshot.Workflows
            .SelectMany(plan => plan.Batches.Select(batch =>
                (Batch: batch, Args: plan.RejectionLevel == RejectionLevel.Activity ? activityArgs : claimArgs)))
            .ToList();

        return Parallel.ForEachAsync(
            work,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken },
            async (item, token) => await item.Batch.EnsureCompiledAsync(snapshot.Engine, item.Args, token).ConfigureAwait(false));
    }

    /// <summary>Caller must hold <see cref="_reloadLock"/>.</summary>
    private async Task BuildAndPublishAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkFlowRepository>();
            var executer = scope.ServiceProvider.GetRequiredService<IAsyncQueryableExecuter>();

            // Fingerprint BEFORE the data: a write in between makes the next check rebuild (safe direction).
            var fingerprint = await repository.GetRulesFingerprintAsync(cancellationToken).ConfigureAwait(false);
            var query = await repository.GetAllForRuleEngine().ConfigureAwait(false);
            var workflows = await executer.ToListAsync(query, cancellationToken).ConfigureAwait(false);

            var snapshot = RulesEngineSnapshotFactory.Create(workflows);

            // Compile everything BEFORE the snapshot becomes visible, so no request ever pays for (or waits on) compilation.
            await WarmUpAsync(snapshot, cancellationToken).ConfigureAwait(false);

            _state = new LoadedState(snapshot, fingerprint);
            Volatile.Write(ref _lastFreshnessCheck, Environment.TickCount64);

            var anchored = snapshot.Workflows.Sum(p => p.AnchoredRuleCount);
            var common = snapshot.Workflows.Sum(p => p.CommonRuleCount);
            _logger.LogInformation(
                "Rules engine loaded: {WorkflowCount} workflow(s); activity-level rules: {Anchored} anchored to specific activity codes (skipped for other activities), {Common} evaluated for every activity",
                snapshot.Workflows.Count, anchored, common);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Failed to load workflows into the rules engine");
            throw;
        }
    }
}
