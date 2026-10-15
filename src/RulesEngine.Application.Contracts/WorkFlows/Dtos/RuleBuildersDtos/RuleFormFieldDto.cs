using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class RuleFormFieldDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string DataType { get; set; } = default!;
        public List<string>? Options { get; set; }
        public List<int>? AllowedOperators { get; set; }
        public bool IsMultiple { get;  set; }

    }
}
