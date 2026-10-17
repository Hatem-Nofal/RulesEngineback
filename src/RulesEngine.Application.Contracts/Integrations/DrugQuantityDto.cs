using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Integrations
{
    public class DrugQuantityDto
    {
        public long Id { get; set; }
        public string DrugName { get; set; }
        public int Quantity { get; set; }
        public long ClaimValidationId { get; set; }
    }
}
