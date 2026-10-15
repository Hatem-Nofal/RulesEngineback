using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace RulesEngine.WorkFlows.Rules
{
    public interface IRuleAppService : ICrudAppService< RuleDto, RuleListItemDto, Guid, PagedAndSortedResultRequestDto, CreateRuleDto, CreateRuleDto>
    {
        Task<RuleExpressionResultDto> PreviewExpression(RuleDto input);
    }
}
