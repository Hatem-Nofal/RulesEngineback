using RulesEngine.Common.Enums;
using RulesEngine.Common.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class RuleNodeDto:EntityDto<Guid>
    {
         public RuleNodeType NodeType { get; set; }
        public LogicalOperator? LogicalOperator { get; set; }
        public LogicalOperator? LogicalJoinOperator { get; set; }
        public string? FieldKey { get; set; }
        public RuleOperator? Operator { get; set; }
        public JsonElement? Value { get; set; }
        public bool IsMultiple { get; set; }
        public long? ActivityTypeId { get; set; }
        public List<RuleNodeDto> Children { get; set; } = new();

      
    }
}
