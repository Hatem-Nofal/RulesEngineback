using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Logers.Dtos
{
    public class RulesEngineExecutionLogListDto
    {
        public string WorkFlowName { get; set; }
        public string? Rule { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ClaimNumber { get; set; }
        public string Expression { get; set; }
        public string Activity { get; set; }
        public DateTime CreationTime { get; set; }
        public bool IsViolated { get; set; } = false;
    }
}
