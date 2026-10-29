using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows
{
    [Table("Workflows")]
    public sealed class WorkFlow : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; private set; } = default!;
        public string NormalizedName { get; private set; }
        public RejectionLevel RejectionLevel { get; private set; }

        public Guid? RuleformConfigId { get; private set; }

        [ForeignKey("RuleformConfigId")]

        public RuleFormConfig RuleformConfig { get; private set; }

        public bool IsActive { get; private set; } = true;
        public bool HasCustomeRule { get; private set; } = false;

        public string? ViolationMessage { get; private set; }

        // EF mapping property - keep private setter and expose read-only view
        public ICollection<Rule> Rules { get; private set; } = new List<Rule>();

        [NotMapped]
        public IReadOnlyCollection<Rule> _Rules => (IReadOnlyCollection<Rule>)Rules;
        public WorkFlow()
        {

        }

        public WorkFlow(string name, Guid? ruleFormConfigId, RejectionLevel rejectionLevel, bool hasCustomeRule, string? violationMessage)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RuleformConfigId = ruleFormConfigId;
            NormalizedName = name.NormalizeSpace();
            RejectionLevel = rejectionLevel;
            HasCustomeRule = hasCustomeRule;
            ViolationMessage = violationMessage;
        }

        public void SetId(Guid? id)
        {
            Id = id ?? Guid.CreateVersion7();
        }
        public void SetHasCustomeRule(bool hasCustomeRule)
        {
            HasCustomeRule = hasCustomeRule;
        }
        public void SetViolationMessage(string? violationMessage)
        {
            ViolationMessage = violationMessage;
        }
        public void AddRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            Rules.Add(rule);
        }
        public void UpdateRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            Rules.FirstOrDefault(x => x.Id == rule.Id).Update(rule.WorkflowId, rule.Expression, rule.ViolationMessage);
        }

        public void RemoveRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            Rules.Remove(rule);
        }

        public void UpdateName(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NormalizedName = name.NormalizeSpace();
        }
        public void UpdateRuleFormConfigId(Guid? ruleFormConfigId)
        {
            RuleformConfigId = ruleFormConfigId;
        }

    }

}
