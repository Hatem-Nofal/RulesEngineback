using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RulesDtos
{
    public class RuleListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public bool IsActive { get; set; }
        public ViolationType ViolationType { get; set; }

    }
}
