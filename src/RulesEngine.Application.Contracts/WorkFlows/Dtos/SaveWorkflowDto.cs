using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos
{
    public class SaveWorkflowDto
    {
        public string Name { get; set; } = default!;
        public long? RuleFormConfigId { get; set; }
        public RejectionLevel RejectionLevel { get;  set; }
        public bool IsWorkflowLevel { get; set; } = false;
        public string RuleExpression { get;  set; }
        public string? ViolationMessage { get; set; }
        public ViolationType ViolationType { get; set; }


    }
}
