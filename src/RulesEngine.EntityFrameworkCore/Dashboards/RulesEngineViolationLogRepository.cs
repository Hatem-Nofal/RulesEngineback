using Microsoft.EntityFrameworkCore;
using RulesEngine.Entities.Logers;
using RulesEngine.EntityFrameworkCore;
using RulesEngine.Logers;
using RulesEngine.Repositories.Logers;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RulesEngine.Dashboards
{
    public class RulesEngineViolationLogRepository : EfCoreRepository<RulesEngineDbContext, RulesEngineExecutionLog, int>,
IRulesEngineViolationLogRepository
    {
        private readonly RulesEngineDbContext _context;

        public RulesEngineViolationLogRepository(IDbContextProvider<RulesEngineDbContext> dbContextProvider, RulesEngineDbContext context) : base(dbContextProvider)
        {
            _context = context;
        }

        public async Task<DashboardDetails> GetDashboardDetails(DashboardFilter input)
        {
            var query = _context.RulesEngineViolationLogs.AsNoTracking();
            query = query
                .Where(x => x.IsViolated).WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate);

            var result = new DashboardDetails
            {
                TotalViolationCount = await query.CountAsync()
            };






            result.TotalClaimNumberViolatedCount = await query.Where(x => x.ClaimNumber != null)
                         .GroupBy(x => x.ClaimNumber)
                         .Select(g => new
                         {
                             ClaimNumber = g.Key,
                             Count = g.Count()
                         })
                         .CountAsync();
            result.TotalWorkFlowViolatedCount = await query.Where(x => x.WorkFlowName != null)
                         .GroupBy(x => x.WorkFlowName)
                         .Select(g => new
                         {
                             WorkFlowName = g.Key,
                             Count = g.Count()
                         })
                          .CountAsync();

            return result;

        }


    }
}
