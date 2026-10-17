using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class CreateRuleDtoToRuleDtoMapper : Volo.Abp.Mapperly.MapperBase<CreateRuleDto, RuleDto>
    {
        public override partial RuleDto Map(CreateRuleDto source);
        public override partial void Map(CreateRuleDto source, RuleDto destination);
    }
}
