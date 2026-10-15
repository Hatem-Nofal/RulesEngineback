using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleNodeDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<RuleNodeDto, RuleNode>
    {
 
        public override partial void Map(RuleNodeDto source, RuleNode destination);

        public override partial  RuleNode Map(RuleNodeDto source);

        public override partial RuleNodeDto ReverseMap(RuleNode source);

        public override partial void ReverseMap(RuleNode source, RuleNodeDto destination);
    }
}

