using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class FormConfigFieldInputDto
    {
        public Guid FieldId { get; set; }
        /// <summary>null / empty = every operator of the field's data type.</summary>
        public List<int>? AllowedOperators { get; set; }

        public bool IsMultiple { get;  set; }

    }
}
