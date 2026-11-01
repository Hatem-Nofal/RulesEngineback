using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards
{
    public class DashboardDetails
    {
        public long TotalViolationCount { get; set; }
    
        public long TotalClaimNumberViolatedCount { get; set; }
        public long TotalWorkFlowViolatedCount { get; set; }
    }
}
