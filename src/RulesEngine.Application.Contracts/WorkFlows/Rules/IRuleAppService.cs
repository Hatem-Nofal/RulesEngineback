using RulesEngine.WorkFlows.Dtos.RulesDtos;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace RulesEngine.WorkFlows.Rules
{
    public interface IRuleAppService : ICrudAppService< RuleDto, RuleListItemDto, Guid, PagedAndSortedResultRequestDto, RuleDto, RuleDto>
    {
    }
}
