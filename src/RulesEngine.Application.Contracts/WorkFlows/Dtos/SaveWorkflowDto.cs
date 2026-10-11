using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos
{
    public class SaveWorkflowDto
    {
        public string Name { get; set; } = default!;
        public Guid RuleFormConfigId { get; set; }
    }
}
