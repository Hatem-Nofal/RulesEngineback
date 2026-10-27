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
        public string? ErrorMessage { get; set; }
        public string? ClaimNumber { get; set; }
        public string? NormalizedWorkFlowName { get; set; }
        public decimal? MaxDailyQuantity { get; set; }
    }
}
