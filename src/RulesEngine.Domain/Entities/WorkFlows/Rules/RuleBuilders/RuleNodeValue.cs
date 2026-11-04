using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleNodeValues", Schema = "RuleBuilders")]
    public sealed class RuleNodeValue : FullAuditedEntity<Guid>
    {
        public long RuleNodeId { get; private set; }
        [ForeignKey(nameof(RuleNodeId))]
        public RuleNode RuleNode { get; private set; }
        public string? ValueJson { get; private set; }
        public long RuleId { get; private set; }
        [ForeignKey(nameof(RuleId))]
        public Rule Rule { get; private set; }

        private RuleNodeValue()
        {

        }
        public RuleNodeValue Create (long ruleNodeId, long ruleId, string? valueJson = null)
        {
            return new RuleNodeValue()
            {
                Id = Guid.NewGuid(),
                RuleNodeId = ruleNodeId,
                RuleId = ruleId,
                ValueJson = valueJson
            };
           
        }

    }
}
