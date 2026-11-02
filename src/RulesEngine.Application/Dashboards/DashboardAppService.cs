using RulesEngine.Dashboards.Dtos;
using RulesEngine.Entities.Logers;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Repositories.Logers;
using RulesEngine.Repositories.WorkFlows;
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
        private readonly IRepository<RulesEngineExecutionLog> _rulesEngineViolationLogRepository;
        private readonly IRulesEngineViolationLogRepository _rulesViolationLogRepository;
        private readonly IRepository<WorkFlow> _workFlowsRepository;
        private readonly IRuleRepository _ruleRepository;


        public DashboardAppService(IRepository<RulesEngineExecutionLog> rulesEngineViolationLogRepository, IRulesEngineViolationLogRepository rulesViolationLogRepository, IRepository<WorkFlow> workFlowsRepository, IRuleRepository ruleRepository)
        {
            _rulesEngineViolationLogRepository = rulesEngineViolationLogRepository;
            _rulesViolationLogRepository = rulesViolationLogRepository;
            _workFlowsRepository = workFlowsRepository;
            _ruleRepository = ruleRepository;
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
                WhereIf(!string.IsNullOrEmpty(input.ClaimNumber), x => x.ClaimNumber.Contains(input.ClaimNumber)).
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

        public async Task<DashboardDetailsDto> GetDashboardDetails(DashboardFilterDto input)
        {
            var violationsDetails = await _rulesViolationLogRepository.GetDashboardDetails(new DashboardFilter()
            {
                Activity = input.Activity,
                FromDate = input.FromDate,
                ToDate = input.ToDate,
                WorkFlowName = input.WorkFlowName
            });

            var totalWorkflowsCount = await _workFlowsRepository.CountAsync();
            var totalRulesCount = await _ruleRepository.CountAsync();
            return new DashboardDetailsDto()
            {
                TotalWorkflowsCount = totalWorkflowsCount,
                TotalRulesCount = totalRulesCount,
                TotalViolationCount = violationsDetails.TotalViolationCount,
                TotalClaimNumberViolatedCount = violationsDetails.TotalClaimNumberViolatedCount,
                TotalWorkFlowViolatedCount = violationsDetails.TotalWorkFlowViolatedCount
            };
        }

        public async Task<WorkflowViolationsDto> GetWorkflowViolationsCounts(DashboardFilterDto input)
        {



            var workflowViolationsCount = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.ClaimNumber), x => x.ClaimNumber.Contains(input.ClaimNumber)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate)
                   .
                    GroupBy(x => x.WorkFlowName).Select(g => new WorkflowViolationsCountDto()
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
