using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Integrations
{
    public class DiagnosisDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public long ClaimValidationId { get; set; }
    }
}
