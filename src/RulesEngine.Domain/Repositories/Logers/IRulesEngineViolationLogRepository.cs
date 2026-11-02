using RulesEngine.Dashboards;
using RulesEngine.Entities.Logers;
using RulesEngine.Logers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.Logers
{
    public interface IRulesEngineViolationLogRepository: IRepository
    {
        Task<DashboardDetails> GetDashboardDetails(DashboardFilter input);
    }
}
