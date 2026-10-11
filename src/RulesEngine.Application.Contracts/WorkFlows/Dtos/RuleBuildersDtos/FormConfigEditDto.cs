using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class FormConfigEditDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AllowNestedGroups { get; set; } = true;
        /// <summary>List order = display order in the builder.</summary>
        public List<FormConfigFieldInputDto> Fields { get; set; } = new();
    }
}
