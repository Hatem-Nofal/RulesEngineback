using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleNodeToRuleNodeDtoMapper : Volo.Abp.Mapperly.MapperBase<RuleNode, RuleNodeDto>
    {
        public override partial RuleNodeDto Map(RuleNode source);

        public override partial void Map(RuleNode source, RuleNodeDto destination);
    }
}
