using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleListItemDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<RuleListItemDto, Rule>
    {
        public override partial Rule Map(RuleListItemDto source);
        public override partial void Map(RuleListItemDto source, Rule destination);

        public override partial RuleListItemDto ReverseMap(Rule source);
        public override partial void ReverseMap(Rule source, RuleListItemDto destination);
    }
}
