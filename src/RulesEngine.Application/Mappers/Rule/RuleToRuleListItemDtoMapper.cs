using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleToRuleListItemDtoMapper : Volo.Abp.Mapperly.MapperBase<Rule, RuleListItemDto>
    {
        public override partial RuleListItemDto Map(Rule source);
        public override partial void Map(Rule source, RuleListItemDto destination);
    }
}
