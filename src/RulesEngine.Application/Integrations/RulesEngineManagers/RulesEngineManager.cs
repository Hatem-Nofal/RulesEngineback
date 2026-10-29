using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RulesEngine.Repositories.WorkFlows;
using System;
using System.Collections.Generic;
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

        if (IsFreshnessCheckDue())
            await RefreshIfChangedAsync(cancellationToken).ConfigureAwait(false);

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

    private bool IsFreshnessCheckDue()
        => Environment.TickCount64 - Volatile.Read(ref _lastFreshnessCheck) >= FreshnessCheckIntervalMs;

    /// <summary>Never throws (other than cancellation): on any failure the current snapshot keeps serving.</summary>
    private async Task RefreshIfChangedAsync(CancellationToken cancellationToken)
    {
        // Someone else is already checking/reloading: just keep using the current snapshot.
        if (!await _reloadLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            if (!IsFreshnessCheckDue())
                return;

            Volatile.Write(ref _lastFreshnessCheck, Environment.TickCount64);

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

            _state = new LoadedState(snapshot, fingerprint);
            Volatile.Write(ref _lastFreshnessCheck, Environment.TickCount64);

            _logger.LogInformation("Rules engine loaded: {WorkflowCount} workflow(s)", snapshot.Workflows.Count);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Failed to load workflows into the rules engine");
            throw;
        }
    }
}
