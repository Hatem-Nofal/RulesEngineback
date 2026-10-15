using RulesEngine.Common.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleNodes", Schema = "RuleBuilders")]
    public sealed class RuleNode : FullAuditedEntity<Guid>
    {
        public Guid RuleId { get; private set; }
        [ForeignKey(nameof(RuleId))]
        public Rule Rule { get; private set; }

        public Guid? ParentId { get; private set; }
        [ForeignKey(nameof(ParentId))]
        public RuleNode Parent { get; private set; }

        public RuleNodeType NodeType { get; private set; }
        public LogicalOperator? LogicalOperator { get; private set; }
        public LogicalOperator? LogicalJoinOperator { get; set; }

        public string? FieldKey { get; private set; }
        public RuleOperator? Operator { get; private set; }
        public string? ValueJson { get; private set; }
        public int Order { get; private set; }

        public RuleNode() { }

        public RuleNode(Guid id, Guid ruleId, RuleNodeType nodeType, int order)
        {
            Id = id;
            RuleId = ruleId;
            NodeType = nodeType;
            Order = order;
        }

        public void SetParent(RuleNode parent)
        {
            Parent = parent;
            ParentId = parent?.Id;
        }

        public void UpdateCriteria(string? fieldKey, RuleOperator? op, string? valueJson, LogicalOperator? logical = null, LogicalOperator? joinOperator = null)
        {
            FieldKey = fieldKey;
            Operator = op;
            ValueJson = valueJson;
            LogicalOperator = logical;
            LogicalJoinOperator = joinOperator;
        }
    }

}
