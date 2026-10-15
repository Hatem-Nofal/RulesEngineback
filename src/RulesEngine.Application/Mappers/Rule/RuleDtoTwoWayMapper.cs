using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<RuleDto, Rule>
    {
        public override partial Rule Map(RuleDto source);
        public override partial void Map(RuleDto source, Rule destination);

        public override partial RuleDto ReverseMap(Rule source);
        public override partial void ReverseMap(Rule source, RuleDto destination);
    }
}
