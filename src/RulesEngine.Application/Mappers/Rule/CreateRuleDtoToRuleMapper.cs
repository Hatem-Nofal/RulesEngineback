using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateRuleDtoToRuleMapper : Volo.Abp.Mapperly.MapperBase<CreateRuleDto, Rule>
    {
        public override partial Rule Map(CreateRuleDto source);
        public override partial void Map(CreateRuleDto source, Rule destination);
    }
}
