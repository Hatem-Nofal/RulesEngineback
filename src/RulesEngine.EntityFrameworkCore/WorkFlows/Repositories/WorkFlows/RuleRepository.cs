using Microsoft.EntityFrameworkCore;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.EntityFrameworkCore;
using RulesEngine.Repositories.WorkFlows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RulesEngine.WorkFlows.Repositories.WorkFlows
{



    public class RuleRepository
: EfCoreRepository<RulesEngineDbContext, Rule, Guid>,
IRuleRepository
    {
        private readonly RulesEngineDbContext _context;

        public RuleRepository(IDbContextProvider<RulesEngineDbContext> dbContextProvider, RulesEngineDbContext context) : base(dbContextProvider)
        {
            _context = context;
        }

        public async Task<List<Rule>> GetListAsync()
        {
            
            var list = await _context.Rules.AsNoTracking().OrderBy(x => x.Name).ToListAsync();

            return list.ToList();
        }

        public async Task<Rule> GetAsync(Guid id)
        {
            var w = await _context.Rules.AsNoTracking().Include(x=>x.Nodes).Include(x=>x.Workflow).ThenInclude(x=>x.Ruleformconfig).ThenInclude(x=>x.RuleFormConfigFields).ThenInclude(x=>x.Rulefield).FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException("Workflow not found.");
            
            return w;
        }
        public async Task<Rule> GetForUpdateAsync(Guid id)
        {
            var w = await _context.Rules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException("Workflow not found.");
            
            return w;
        }

        


    }
}
