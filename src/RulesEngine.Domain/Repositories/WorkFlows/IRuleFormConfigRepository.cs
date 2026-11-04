using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.WorkFlows
{
    public interface IRuleFormConfigRepository:IRepository
    {
        Task<List<RuleFormConfig>> GetConfigsAsync();
         Task<RuleFormConfig?> GetByWorkflowAsync(long workflowId);
        Task<List<RuleFormConfig>> GetListAsync();
        Task<RuleFormConfig> GetAsync(long id);
        Task<RuleFormConfig> CreateAsync(RuleFormConfig input);
        Task<RuleFormConfig> UpdateAsync(long id, RuleFormConfig input);
        Task DeleteAsync(long id);

        Task<IQueryable<RuleFormConfig>> GetQueryableAsync();
    }
}
