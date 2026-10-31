using Microsoft.EntityFrameworkCore;
using RulesEngine.Entities.Logers;
using RulesEngine.EntityFrameworkCore;
using RulesEngine.Repositories.Logers;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RulesEngine.Dashboards
{
    public class RulesEngineViolationLogRepository : EfCoreRepository<RulesEngineDbContext, RulesEngineViolationLog, int>,
IRulesEngineViolationLogRepository
    {
        private readonly RulesEngineDbContext _context;

        public RulesEngineViolationLogRepository(IDbContextProvider<RulesEngineDbContext> dbContextProvider, RulesEngineDbContext context) : base(dbContextProvider)
        {
            _context = context;
        }

        public async Task<ViolationsDetails> GetViolationsDetails(DashboardFilter input)
        {
            var query = _context.RulesEngineViolationLogs.AsNoTracking();
            query = query.WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate);

            var result = new ViolationsDetails
            {
                TotalViolationCount = await query.CountAsync()
            };

            // Most violated Workflow
            var workflow = await query
                .GroupBy(x => x.WorkFlowName)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            result.MaxWorkFlowNameViolated = workflow?.Name;
            result.MaxWorkFlowViolatedCount = workflow?.Count ?? 0;

            // Most violated Rule
            var rule = await query
                .Where(x => x.Rule != null)
                .GroupBy(x => x.Rule)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            result.MaxRuleNameViolated = rule?.Name;
            result.MaxRuleViolatedCount = rule?.Count ?? 0;

            // Most violated Activity
            var activity = await query
                .GroupBy(x => x.Activity)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            result.MaxActivityViolated = activity?.Name;
            result.MaxActivityViolatedCount = activity?.Count ?? 0;

            // Most violated Claim Number
            var claim = await query
                .Where(x => x.ClaimNumber != null)
                .GroupBy(x => x.ClaimNumber)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            result.MaxClaimNumberViolated = claim?.Name;
            result.MaxClaimNumberViolatedCount = claim?.Count ?? 0;

            return result;
        }
    }
}
