using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RulesEngine.Application.Repositories.WorkFlows
{
    public interface IRuleFormConfigRepository
    {
        Task<List<RuleFormConfig>> GetConfigsAsync();
         Task<RuleFormConfig?> GetByWorkflowAsync(Guid workflowId);
        Task<List<RuleFormConfig>> GetListAsync();
        Task<RuleFormConfig> GetAsync(Guid id);
        Task<RuleFormConfig> CreateAsync(RuleFormConfig input);
        Task<RuleFormConfig> UpdateAsync(Guid id, RuleFormConfig input);
        Task DeleteAsync(Guid id);

    }
}
