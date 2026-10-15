using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class FormConfigListDto:EntityDto<Guid>
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool AllowNestedGroups { get; set; }
        public string? StrategyKey { get; set; }
        public int FieldCount { get; set; }
        public int WorkflowCount { get; set; }
    }
}
