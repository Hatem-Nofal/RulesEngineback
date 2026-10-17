using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Integrations
{
    public class ClaimValidationResultDto
    {
        public int ActivityId { get; set; }
        public string WorkFlowName { get; set; }
        public string? Rule { get; set; }
        public string? ErrorType { get; set; }
        public string? ClaimNumber { get; set; }
        public List<string>? MatchedActivityCodes { get; set; }
        public ClaimDto? Claim { get; set; }
        public string? NormalizedWorkFlowName { get; set; }
    }
}
