using System.Threading;
using System.Threading.Tasks;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

public interface IRulesEngineManager
{
    /// <summary>
    /// Returns the current immutable engine snapshot (engine + workflow metadata).
    /// Prefer this over <see cref="GetRulesEngineAsync"/>: it needs no database access per call.
    /// </summary>
    Task<RulesEngineSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<RulesEngine> GetRulesEngineAsync();

    /// <summary>Rebuilds the snapshot from the database immediately.</summary>
    Task ReloadWorkflowsAsync();

    /// <summary>
    /// Call from any code that changes workflows or rules. The reload runs once, after the current
    /// unit of work commits (or immediately when there is none), so it always sees the new data.
    /// </summary>
    Task ReloadAfterCommitAsync();

    int GetBatchCount(string baseWorkflowName);
}
