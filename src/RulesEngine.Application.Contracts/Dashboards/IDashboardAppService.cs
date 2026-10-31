using RulesEngine.Dashboards.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Dashboards
{
    public interface IDashboardAppService
    {
        Task<WorkflowViolationsDto> GetWorkflowViolationsCounts(DashboardFilterDto input);
        Task<ClaimNumberViolationsDto> GetClaimNumberViolationsCounts(DashboardFilterDto input);
        Task<ActivityViolationsDto> GetActivityViolationsCounts(DashboardFilterDto input);
        Task<RuleViolationsDto> GetRuleViolationsCounts(DashboardFilterDto input);
        Task<ViolationsDetailsDto> GetViolationsDetails(DashboardFilterDto input);
    }
}
