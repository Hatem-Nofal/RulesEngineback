using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class RuleFormConfigDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public bool AllowNestedGroups { get; set; }
        public string? StrategyKey { get; set; }
 
        public List<RuleFormFieldDto> Fields { get; set; } = new();
    }
}
