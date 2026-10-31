using RulesEngine.Dashboards.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Dashboards
{
    public interface IDashboardAppService
    {
        Task<WorkflowViolationsDto> GeetWorkflowViolationsCounts();
        Task<ClaimNumberViolationsDto> GeetClaimNumberViolationsCounts();
        Task<ActivityViolationsDto> GeetActivityViolationsCounts();
        Task<RuleViolationsDto> GeetRuleViolationsCounts();
    }
}
