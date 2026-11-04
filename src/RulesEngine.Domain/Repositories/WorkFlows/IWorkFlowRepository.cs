using RulesEngine.Entities.WorkFlows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.WorkFlows
{
    public interface IWorkFlowRepository:IRepository<WorkFlow, long>
    {
        Task<List<WorkFlow>> GetListAsync();
        Task<WorkFlow> GetAsync(long id);
        Task<long> CreateAsync(WorkFlow input);
        Task CreateRulesAsync(WorkFlow input);
        Task UpdateAsync(long id, WorkFlow input);
        Task DeleteAsync(long id);
        Task<IQueryable<WorkFlow>> GetAllForRuleEngine();
        Task<IQueryable<WorkFlow>> GetAllList();
        Task<RulesFingerprint> GetRulesFingerprintAsync(CancellationToken cancellationToken = default);
        Task<WorkFlow> GetDetailsAsync(long id);
        Task<WorkFlow> GetForUpdateAsync(long id);

    }
    public readonly record struct RulesFingerprint(int Workflows, int Rules, DateTime? LastChange);

}
