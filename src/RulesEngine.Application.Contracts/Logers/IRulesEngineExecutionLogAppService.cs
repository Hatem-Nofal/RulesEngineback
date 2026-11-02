using RulesEngine.Dashboards.Dtos;
using RulesEngine.Logers.Dtos;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.Logers
{
    public interface IRulesEngineExecutionLogAppService
    {
        Task<PagedResultDto<RulesEngineExecutionLogListDto>> GetRulesEngineExecutionLogList(RulesEngineExecutionLogFiltersDto input);

    }
}
