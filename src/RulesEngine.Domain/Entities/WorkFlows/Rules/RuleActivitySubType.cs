using RulesEngine.Entities.Lookups;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    [Table("RuleActivitySubTypes")]
    public sealed class RuleActivitySubType : Entity<long>
    {
        public Guid RuleId { get; private set; }

        [ForeignKey(nameof(RuleId))]

        public Rule Rule { get; private set; }

        public long ActivitySubTypeId { get; private set; }
        [ForeignKey(nameof(ActivitySubTypeId))]
        public ActivitySubType Activity_Sub_Type { get; private set; }
        public bool? IsSecondary { get; private set; }

 
        public RuleActivitySubType(long id, Guid ruleId, long activitySubTypeId, bool? isSecondary = null)
        {
            Id = id;
            RuleId = ruleId;
            ActivitySubTypeId = activitySubTypeId;
            IsSecondary = isSecondary;
        }
    }
}
