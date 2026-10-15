using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Mappers.RuleFormConfigMapper
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFormConfigToRuleFormConfigDtoMapper
   : Volo.Abp.Mapperly.MapperBase<RuleFormConfig, RuleFormConfigDto>
    {
        public override partial RuleFormConfigDto Map(RuleFormConfig source);
        public override partial void Map(RuleFormConfig source, RuleFormConfigDto destination);
    }

}
