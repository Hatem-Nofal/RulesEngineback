using RulesEngine.Entities.WorkFlows;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.WorkFlows
{
    public interface IWorkFlowRepository:IRepository<WorkFlow, Guid>
    {
        Task<List<WorkFlow>> GetListAsync();
        Task<WorkFlow> GetAsync(Guid id);
        Task<Guid> CreateAsync(WorkFlow input);
        Task UpdateAsync(Guid id, WorkFlow input);
        Task DeleteAsync(Guid id);
     }
}
 