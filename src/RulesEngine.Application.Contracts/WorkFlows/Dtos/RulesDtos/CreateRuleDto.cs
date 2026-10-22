using RulesEngine.Common.Enums;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RulesDtos
{
    public class CreateRuleDto
    {
         public Guid WorkflowId { get; set; }
        public bool IsActive { get; set; } = true;
 
        public RuleNodeDto Root { get; set; } = default!;
        public ViolationType ViolationType { get; set; }

    }
}
