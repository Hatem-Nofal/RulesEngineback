using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards.Dtos
{
    public class DashboardFilterDto
    {
        public string? WorkFlowName { get;  set; }
        public DateTime? FromDate { get;  set; }
        public DateTime? ToDate { get;  set; }
        public string? Activity { get;  set; }

    }
}
