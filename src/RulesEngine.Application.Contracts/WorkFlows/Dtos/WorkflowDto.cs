using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos
{
    public class WorkflowDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid? RuleFormConfigId { get; set; }
        public string? RuleFormConfigName { get; set; }
        public int RuleCount { get; set; }
        public RejectionLevel RejectionLevel { get;  set; }
        public bool HasCustomeRule { get; set; } = false;
        public string? RuleExpression { get;  set; }
        public string? ViolationMessage { get;  set; }


    }
}
