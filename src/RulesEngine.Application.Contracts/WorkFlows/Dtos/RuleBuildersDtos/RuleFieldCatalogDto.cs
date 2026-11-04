using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class RuleFieldCatalogDto
    {
        public long Id { get; set; }
        public string Key { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string DataType { get; set; } = default!;
 
        public List<string>? Options { get; set; }
    }
}
