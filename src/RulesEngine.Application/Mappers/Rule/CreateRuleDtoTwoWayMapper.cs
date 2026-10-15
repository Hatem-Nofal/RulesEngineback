using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateRuleDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<CreateRuleDto, Rule>
    {
        public override partial Rule Map(CreateRuleDto source);
        public override partial void Map(CreateRuleDto source, Rule destination);

        public override partial CreateRuleDto ReverseMap(Rule source);
        public override partial void ReverseMap(Rule source, CreateRuleDto destination);
    }
}
