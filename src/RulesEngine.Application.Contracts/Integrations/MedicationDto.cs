using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Integrations
{
    public class MedicationDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long ClaimValidationId { get; set; }
    }
}
