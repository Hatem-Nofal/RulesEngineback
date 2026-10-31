using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards.Dtos
{
    public class WorkflowViolationsCountDto
    {
        public string WorkFlowName { get; set; }
        public long ViolationsCounts { get; set; }
    }
    public class WorkflowViolationsDto
    {
        public List<WorkflowViolationsCountDto> WorkflowViolationsCount { get; set; }
        public long Total { get; set; }
    }
}
