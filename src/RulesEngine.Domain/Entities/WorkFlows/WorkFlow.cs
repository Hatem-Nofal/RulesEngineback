using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows
{
    [Table("Workflows")]
    public sealed class WorkFlow : FullAuditedAggregateRoot<long>
    {
        public string Name { get; private set; } = default!;
        public string NormalizedName { get; private set; }
        public RejectionLevel RejectionLevel { get; private set; }

        public long? RuleformConfigId { get; private set; }

        [ForeignKey("RuleformConfigId")]

        public RuleFormConfig RuleformConfig { get; private set; }

        public bool IsActive { get; private set; } = true;
        public bool IsWorkflowLevel { get; private set; } = false;
        public ViolationType ViolationType { get; private set; } = ViolationType.Error;

        public string? ViolationMessage { get; private set; }

        // EF mapping property - keep private setter and expose read-only view
      
        private readonly List<Rule> _rules = new();

        public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();
        // EF mapping property - keep list with private setter
        public List<RuleNode> Nodes { get; private set; } = new();

        [NotMapped]
        public IReadOnlyCollection<RuleNode> _Nodes => Nodes.AsReadOnly();
        public WorkFlow()
        {

        }

        public WorkFlow(string name, long? ruleFormConfigId, RejectionLevel rejectionLevel, bool isWorkflowLevel, string? violationMessage, ViolationType violationType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RuleformConfigId = ruleFormConfigId;
            NormalizedName = name.NormalizeSpace();
            RejectionLevel = rejectionLevel;
            IsWorkflowLevel = isWorkflowLevel;
            ViolationMessage = violationMessage;
            ViolationType = violationType;
        }

       
        public void SetIsWorkflowLevel(bool isWorkflowLevel)
        {
            IsWorkflowLevel = isWorkflowLevel;
        }
        public void SetViolationMessage(string? violationMessage)
        {
            ViolationMessage = violationMessage;
        }
        public void SetViolationType(ViolationType violationType)
        {
            ViolationType = violationType;
        }
        public void AddRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules.Add(rule);
        }
        public void UpdateRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules.FirstOrDefault(x => x.Id == rule.Id)?.Update(rule.Expression, rule.ViolationMessage, rule.ViolationType);
        }

        public void RemoveRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _rules.Remove(rule);
        }

        public void UpdateName(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NormalizedName = name.NormalizeSpace();
        }
        public void UpdateRuleFormConfigId(long? ruleFormConfigId)
        {
            RuleformConfigId = ruleFormConfigId;
        }
        public void AddNode(RuleNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Nodes.Add(node);
        }
        public void AddNodes(List<RuleNode> nodes)
        {
            Nodes.AddRange(nodes);
        }

        public void RemoveNode(RuleNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Nodes.Remove(node);
        }
        public void ResetNodes()
        {

            Nodes.Clear();
        }
    }

}
