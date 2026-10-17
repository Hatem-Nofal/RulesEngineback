using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Integrations
{
    public class ClaimValidationResponse
    {
        public string Success { get; set; } = "";
        public List<ClaimValidationResultDto>? Failed { get; set; } = new();
    }
}
