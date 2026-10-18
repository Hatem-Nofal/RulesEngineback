using RulesEngine.Entities.WorkFlows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.WorkFlows
{
    public interface IWorkFlowRepository:IRepository<WorkFlow, Guid>
    {
        Task<List<WorkFlow>> GetListAsync();
        Task<WorkFlow> GetAsync(Guid id);
        Task<Guid> CreateAsync(WorkFlow input);
        Task CreateRulesAsync(WorkFlow input);
        Task UpdateAsync(Guid id, WorkFlow input);
        Task DeleteAsync(Guid id);
        Task<IQueryable<WorkFlow>> GetAllForRuleEngine();
        Task<RulesFingerprint> GetRulesFingerprintAsync(CancellationToken cancellationToken = default);

    }
    public readonly record struct RulesFingerprint(int Workflows, int Rules, DateTime? LastChange);

}
