using RulesEngine.Lookups.Dtos;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace RulesEngine.WorkFlows.Rules.RuleBuilders
{
    public interface IRuleFormConfigAppService : ICrudAppService< RuleFormConfigDto, FormConfigListDto, long, PagedAndSortedResultRequestDto, CreateFormConfigEditDto, CreateFormConfigEditDto>
    {
        Task<RuleFormConfigDto?> GetByWorkflowAsync(long workflowId);
        Task<List<LookupDto>> GetConfigsAsync();
    }
}
