using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards.Dtos
{
    public class RuleViolationsDto
    {
        public List<RuleViolationsCountDto> RuleViolationsCounts { get; set; }
        public long Total { get; set; }
    }
    public class RuleViolationsCountDto
    {
        public string RuleName { get; set; }
        public long ViolationsCounts { get; set; }
    }
}
