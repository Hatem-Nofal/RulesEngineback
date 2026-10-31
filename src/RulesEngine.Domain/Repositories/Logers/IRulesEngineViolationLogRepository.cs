using RulesEngine.Dashboards;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Repositories.Logers
{
    public interface IRulesEngineViolationLogRepository: IRepository
    {
        Task<ViolationsDetails> GetViolationsDetails(DashboardFilter input);
    }
}
