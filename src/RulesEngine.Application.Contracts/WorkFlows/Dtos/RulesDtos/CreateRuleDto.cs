using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RulesDtos
{
    public class CreateRuleDto
    {
        public string Name { get; set; } = default!;
        public Guid WorkflowId { get; set; }
        public bool IsActive { get; set; } = true;
 
        public RuleNodeDto Root { get; set; } = default!;
    }
}
