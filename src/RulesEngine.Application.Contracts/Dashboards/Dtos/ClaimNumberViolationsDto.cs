using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Dashboards.Dtos
{
    public class ClaimNumberViolationsDto
    {

        public List<ClaimNumberViolationsCountDto> ClaimNumberViolationsCount { get; set; }
        public long Total { get; set; }
    }
    public class ClaimNumberViolationsCountDto
    {
        public string ClaimNumber { get; set; }
        public long ViolationsCounts { get; set; }
    }
   
}
