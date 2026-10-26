using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Mappers.RuleFormConfigMapper
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class FormConfigListDtoTwoWayMapper
: Volo.Abp.Mapperly.TwoWayMapperBase<FormConfigListDto, RuleFormConfig>
    {
        public override partial RuleFormConfig Map(FormConfigListDto source);

        public override partial void Map(FormConfigListDto source, RuleFormConfig destination);

        public override partial FormConfigListDto ReverseMap(RuleFormConfig source);

        public override void ReverseMap(RuleFormConfig source, FormConfigListDto destination)
        {
            destination.AllowNestedGroups = source.AllowNestedGroups;
            destination.Description = source.Description;
            destination.FieldCount = source._RuleFormConfigFields.Count;
            destination.WorkflowCount = source._WorkFlows.Count;
            destination.Name = source.Name;
            destination.StrategyKey = source.StrategyKey;
            destination.IsActive = source.IsActive;
            destination.Id = source.Id;


        }
    }
}
