using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RulesEngine.WorkFlows.Dtos.RuleBuildersDtos
{
    public class RuleNodeDto
    {
        public Guid Id { get; set; }
        public RuleNodeType NodeType { get; set; }
        public LogicalOperator? LogicalOperator { get; set; }
        public LogicalOperator? LogicalJoinOperator { get; set; }
        public string? FieldKey { get; set; }
        public RuleOperator? Operator { get; set; }
        public JsonElement? Value { get; set; }
        public bool IsMultiple { get; set; }
        public int? Type { get; set; }
        public List<RuleNodeDto> Children { get; set; } = new();
    }
}
