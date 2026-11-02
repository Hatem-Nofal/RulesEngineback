using RulesEngine.Entities.Logers;
using RulesEngine.Logers.Dtos;
using RulesEngine.WorkFlows.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Logers
{
    public class RulesEngineExecutionLogAppService : ApplicationService, IRulesEngineExecutionLogAppService
    {
        private readonly IRepository<RulesEngineExecutionLog> _rulesEngineViolationLogRepository;

        public RulesEngineExecutionLogAppService(IRepository<RulesEngineExecutionLog> rulesEngineViolationLogRepository)
        {
            _rulesEngineViolationLogRepository = rulesEngineViolationLogRepository;
        }

        public async Task<PagedResultDto<RulesEngineExecutionLogListDto>> GetRulesEngineExecutionLogList(RulesEngineExecutionLogFiltersDto input)
        {
            var query = (await _rulesEngineViolationLogRepository.GetQueryableAsync()).WhereIf(!string.IsNullOrEmpty(input.Activity), x => x.Activity.Contains(input.Activity)).
                WhereIf(!string.IsNullOrEmpty(input.WorkFlowName), x => x.WorkFlowName.Contains(input.WorkFlowName)).
                WhereIf(!string.IsNullOrEmpty(input.RuleName), x => x.Rule.Contains(input.RuleName)).
                WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate).
                WhereIf(input.ToDate.HasValue, x => x.CreationTime <= input.ToDate);

            var total = await AsyncExecuter.CountAsync(query);
            query = query.OrderBy(input.Sorting ?? "CreationTime DESC")
                .PageBy(input);


            return new PagedResultDto<RulesEngineExecutionLogListDto>(total, query.ToList().Select(w => new RulesEngineExecutionLogListDto()
            {
                Activity = w.Activity,
                ClaimNumber = w.ClaimNumber,
                CreationTime = w.CreationTime,
                ErrorMessage = w.ErrorMessage,
                ErrorType = w.ErrorType,
                Expression = w.Expression,
                IsViolated = w.IsViolated,
                WorkFlowName = w.WorkFlowName,
                Rule = w.Rule

            }).ToList());

        }
    }
}
