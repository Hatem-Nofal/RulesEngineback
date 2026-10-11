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
    public interface IRuleFormConfigAppService : ICrudAppService< RuleFormConfigDto, FormConfigListDto, Guid, PagedAndSortedResultRequestDto, FormConfigEditDto, FormConfigEditDto>
    {
        Task<RuleFormConfigDto?> GetByWorkflowAsync(Guid workflowId);
        Task<List<LookupDto>> GetConfigsAsync();
    }
}
