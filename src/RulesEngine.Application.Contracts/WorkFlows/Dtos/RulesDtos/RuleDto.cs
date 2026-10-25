using RulesEngine.Common.Enums;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RulesDtos
{
    public class RuleDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid WorkflowId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsMultiple { get; set; }
        public string? Expression { get; set; }
        public bool WorkflowHasCustomeRule { get;   set; } = false;
        public ViolationType ViolationType { get; set; }
        public string? ViolationMessage { get; set; } = string.Empty;

        public RuleNodeDto Root { get; set; } = default!;
    }
}
