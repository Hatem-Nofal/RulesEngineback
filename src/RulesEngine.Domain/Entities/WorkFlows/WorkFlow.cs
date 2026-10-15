using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows
{
    [Table("Workflows")]
    public sealed class WorkFlow : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; private set; } = default!;
        public string NormalizedName { get; private set; }
        public RejectionLevel RejectionLevel { get; private set; }

        public Guid RuleformconfigId { get; private set; }
        [ForeignKey(nameof(RuleformconfigId))]

        public RuleFormConfig Ruleformconfig { get; private set; }

        // EF mapping property - keep private setter and expose read-only view
        public ICollection<Rule> Rules { get; private set; } = new List<Rule>();

        [NotMapped]
        public IReadOnlyCollection<Rule> _Rules => (IReadOnlyCollection<Rule>)Rules;
        public WorkFlow()
        {
            
        }

        public WorkFlow(  string name, Guid ruleFormConfigId, RejectionLevel rejectionLevel)
        {
             Name = name ?? throw new ArgumentNullException(nameof(name));
            RuleformconfigId = ruleFormConfigId;
            NormalizedName = name.NormalizeSpace();
            RejectionLevel = rejectionLevel;
        }

        public void SetId(Guid? id)
        {
            Id = id??Guid.CreateVersion7();
        }

        public void AddRule(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            Rules.Add(rule);
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
        public void UpdateRuleFormConfigId(Guid ruleFormConfigId)
        {
            RuleformconfigId = ruleFormConfigId;
        }

     }

}
