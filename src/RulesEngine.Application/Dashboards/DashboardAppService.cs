using RulesEngine.Dashboards.Dtos;
using RulesEngine.Entities.Logers;
using RulesEngine.Repositories.Logers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Dashboards
{
    public class DashboardAppService : ApplicationService, IDashboardAppService
    {
        private readonly IRepository<RulesEngineViolationLog> _rulesEngineViolationLogRepository;
        private readonly IRulesEngineViolationLogRepository _rulesViolationLogRepository;

        public DashboardAppService(IRepository<RulesEngineViolationLog> rulesEngineViolationLogRepository, IRulesEngineViolationLogRepository rulesViolationLogRepository)
        {
            _rulesEngineViolationLogRepository = rulesEngineViolationLogRepository;
            _rulesViolationLogRepository = rulesViolationLogRepository;
        }

        public async Task<ActivityViolationsDto> GetActivityViolationsCounts(DashboardFilterDto input)
        {
            var activityViolationsCounts = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate)
                   .GroupBy(x => x.Activity).Select(g => new ActivityViolationsCountDto()
                   {
                       Activity = g.Key,
                       ViolationsCounts = g.LongCount()
                   }).ToList();

            return new ActivityViolationsDto()
            {
                ActivityViolationsCounts = activityViolationsCounts,
                Total = activityViolationsCounts.Select(x => x.ViolationsCounts).Sum()
            };
        }

        public async Task<ClaimNumberViolationsDto> GetClaimNumberViolationsCounts(DashboardFilterDto input)
        {
            var claimNumberViolationsCounts = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate)
                   .
                    GroupBy(x => x.ClaimNumber).Select(g => new ClaimNumberViolationsCountDto()
                    {
                        ClaimNumber = g.Key,
                        ViolationsCounts = g.LongCount()
                    }).ToList();

            return new ClaimNumberViolationsDto()
            {
                ClaimNumberViolationsCount = claimNumberViolationsCounts,
                Total = claimNumberViolationsCounts.Select(x => x.ViolationsCounts).Sum()
            };
        }

        public async Task<RuleViolationsDto> GetRuleViolationsCounts(DashboardFilterDto input)
        {


            var ruleViolationsCount = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate).
                    GroupBy(x => x.Rule).Select(g => new RuleViolationsCountDto()
                    {
                        RuleName = g.Key,
                        ViolationsCounts = g.LongCount()
                    }).ToList();

            return new RuleViolationsDto()
            {
                RuleViolationsCounts = ruleViolationsCount,
                Total = ruleViolationsCount.Select(x => x.ViolationsCounts).Sum()
            };
        }

        public async Task<ViolationsDetailsDto> GetViolationsDetails(DashboardFilterDto input)
        {
            var violationsDetails = await _rulesViolationLogRepository.GetViolationsDetails(new DashboardFilter()
            {
                Activity = input.Activity,
                FromDate = input.FromDate,
                ToDate = input.ToDate,
                WorkFlowName = input.WorkFlowName
            });


            return new ViolationsDetailsDto()
            {
                MaxActivityViolated = violationsDetails.MaxActivityViolated,
                MaxActivityViolatedCount = violationsDetails.MaxActivityViolatedCount,
                MaxClaimNumberViolated = violationsDetails.MaxClaimNumberViolated,
                MaxClaimNumberViolatedCount = violationsDetails.MaxClaimNumberViolatedCount,
                MaxRuleNameViolated = violationsDetails.MaxRuleNameViolated,
                MaxRuleViolatedCount = violationsDetails.MaxRuleViolatedCount,
                MaxWorkFlowNameViolated = violationsDetails.MaxWorkFlowNameViolated,
                MaxWorkFlowViolatedCount = violationsDetails.MaxWorkFlowViolatedCount,
                TotalViolationCount = violationsDetails.TotalViolationCount
            };
        }

        public async Task<WorkflowViolationsDto> GetWorkflowViolationsCounts(DashboardFilterDto input)
        {



            var workflowViolationsCount = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate)
                   .
                    GroupBy(x => x.Rule).Select(g => new WorkflowViolationsCountDto()
                    {
                        WorkFlowName = g.Key,
                        ViolationsCounts = g.LongCount()
                    }).ToList();

            return new WorkflowViolationsDto()
            {
                WorkflowViolationsCount = workflowViolationsCount,
                Total = workflowViolationsCount.Select(x => x.ViolationsCounts).Sum()
            };
        }
    }
}
