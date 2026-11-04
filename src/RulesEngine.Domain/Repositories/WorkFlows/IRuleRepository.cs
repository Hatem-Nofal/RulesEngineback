using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.WorkFlows
{
    public interface IRuleRepository : IRepository<Rule, long>
    {
        Task<List<Rule>> GetListAsync();
        Task<Rule> GetAsync(long id);
        Task<Rule> GetForUpdateAsync(long id);


     }
}
 