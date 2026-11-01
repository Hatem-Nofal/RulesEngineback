using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards.Dtos
{
    public class DashboardDetailsDto
    {
    
        public long TotalViolationCount { get;  set; }
        public long TotalWorkflowsCount { get;  set; }
        public long TotalRulesCount { get;  set; }
        public long TotalClaimNumberViolatedCount { get;  set; }
        public long TotalWorkFlowViolatedCount { get;  set; }
    }
}
