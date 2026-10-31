using RulesEngine.Dashboards.Dtos;
using RulesEngine.Entities.Logers;
using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Dashboards
{
    public class DashboardAppService : ApplicationService, IDashboardAppService
    {
        private readonly IRepository<RulesEngineViolationLog> _rulesEngineViolationLogRepository;

        public DashboardAppService(IRepository<RulesEngineViolationLog> rulesEngineViolationLogRepository)
        {
            _rulesEngineViolationLogRepository = rulesEngineViolationLogRepository;
        }

        public async Task<ActivityViolationsDto> GeetActivityViolationsCounts()
        {
            var activityViolationsCounts = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                    GroupBy(x => x.Activity).Select(g => new ActivityViolationsCountDto()
                    {
                        Activity = g.Key,
                        ViolationsCounts = g.Count()
                    }).ToList();

            return new ActivityViolationsDto()
            {
                ActivityViolationsCounts = activityViolationsCounts,
                Total = activityViolationsCounts.Select(x => x.ViolationsCounts).Sum()
            };
        }

        public async Task<ClaimNumberViolationsDto> GeetClaimNumberViolationsCounts()
        {
            var claimNumberViolationsCounts = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                    GroupBy(x => x.ClaimNumber).Select(g => new ClaimNumberViolationsCountDto()
                    {
                        ClaimNumber = g.Key,
                        ViolationsCounts = g.Count()
                    }).ToList();

            return new ClaimNumberViolationsDto()
            {
                ClaimNumberViolationsCount = claimNumberViolationsCounts,
                Total = claimNumberViolationsCounts.Select(x => x.ViolationsCounts).Sum()
            };
        }

        public async Task<RuleViolationsDto> GeetRuleViolationsCounts()
        {


            var ruleViolationsCount = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                    GroupBy(x => x.Rule).Select(g => new RuleViolationsCountDto()
                    {
                        RuleName = g.Key,
                        ViolationsCounts = g.Count()
                    }).ToList();

            return new RuleViolationsDto()
            {
                RuleViolationsCounts = ruleViolationsCount,
                Total = ruleViolationsCount.Select(x => x.ViolationsCounts).Sum()
            };
        }

        public async Task<WorkflowViolationsDto> GeetWorkflowViolationsCounts()
        {
            


            var workflowViolationsCount= (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                    GroupBy(x => x.Rule).Select(g => new WorkflowViolationsCountDto()
                    {
                        WorkFlowName = g.Key,
                        ViolationsCounts = g.Count()
                    }).ToList();

            return new WorkflowViolationsDto()
            {
                WorkflowViolationsCount = workflowViolationsCount,
                Total = workflowViolationsCount.Select(x => x.ViolationsCounts).Sum()
            };
        }
    }
}
