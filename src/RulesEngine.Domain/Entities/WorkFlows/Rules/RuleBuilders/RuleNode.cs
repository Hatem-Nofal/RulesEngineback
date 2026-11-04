using RulesEngine.Common.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleNodes", Schema = "RuleBuilders")]
    public sealed class RuleNode : FullAuditedEntity<Guid>
    {
        public long WorkFlowId { get; private set; }
        [ForeignKey(nameof(WorkFlowId))]
        public WorkFlow WorkFlow { get; private set; }

        public Guid? ParentId { get; private set; }
        [ForeignKey(nameof(ParentId))]
        public RuleNode Parent { get; private set; }

        public RuleNodeType NodeType { get; private set; }
        public LogicalOperator? LogicalOperator { get; private set; }
        public LogicalOperator? LogicalJoinOperator { get; set; }

        public string? FieldKey { get; private set; }
        public RuleOperator? Operator { get; private set; }
         public int Order { get; private set; }

        public RuleNode() { }

        public RuleNode(long workFlowId,  Guid? parentId,  RuleNodeType nodeType, LogicalOperator? logicalOperator, LogicalOperator? logicalJoinOperator, string? fieldKey, RuleOperator? @operator,   int order)
        {
            Id = Guid.CreateVersion7();

            WorkFlowId = workFlowId;
            ParentId = parentId;
            NodeType = nodeType;
            LogicalOperator = logicalOperator;
            LogicalJoinOperator = logicalJoinOperator;
            FieldKey = fieldKey;
            Operator = @operator;
             Order = order;
        }

        public void SetParent(RuleNode parent)
        {
            Parent = parent;
            ParentId = parent?.Id;
        }

        public void UpdateCriteria(string? fieldKey, RuleOperator? op,   LogicalOperator? logical = null, LogicalOperator? joinOperator = null)
        {
            FieldKey = fieldKey;
            Operator = op;
             LogicalOperator = logical;
            LogicalJoinOperator = joinOperator;
        }
    }

}
