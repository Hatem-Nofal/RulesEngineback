using System.Collections.Generic;

namespace RulesEngine.Dashboards.Dtos
{
  
    public class ActivityViolationsDto
    {
        public List<ActivityViolationsCountDto> ActivityViolationsCounts { get; set; }
        public long Total { get; set; }
    }

    public class ActivityViolationsCountDto
    {
        public string Activity { get; set; }
        public long ViolationsCounts { get; set; }
    }

}
