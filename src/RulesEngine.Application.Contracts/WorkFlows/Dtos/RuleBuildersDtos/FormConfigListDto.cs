using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class FormConfigListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool AllowNestedGroups { get; set; }
        public string? StrategyKey { get; set; }
        public int FieldCount { get; set; }
        public int WorkflowCount { get; set; }
    }
}
