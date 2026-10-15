using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RulesDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleToRuleDtoMapper : Volo.Abp.Mapperly.MapperBase<Rule, RuleDto>
    {
        public override partial RuleDto Map(Rule source);
        public override partial void Map(Rule source, RuleDto destination);
    }
}
