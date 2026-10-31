using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards.Dtos
{
    public class ViolationsDetailsDto
    {
        public string MaxWorkFlowNameViolated { get;  set; }
        public long MaxWorkFlowViolatedCount { get;  set; }
        public string MaxRuleNameViolated { get;  set; }
        public long MaxRuleViolatedCount { get;  set; }
        public string MaxActivityViolated { get;  set; }
        public long MaxActivityViolatedCount { get;  set; }
        public string MaxClaimNumberViolated { get;  set; }
        public long MaxClaimNumberViolatedCount { get;  set; }
        public long TotalViolationCount { get;  set; }
    }
}
