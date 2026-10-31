using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards
{
    public class DashboardFilter
    {
        public string? WorkFlowName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Activity { get; set; }
    }
}
