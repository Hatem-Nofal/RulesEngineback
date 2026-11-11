using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    [Table("Rules")]
    public sealed class Rule : FullAuditedEntity<long>
    {
        public string Name { get; private set; }

        public long WorkflowId { get; private set; }

        [ForeignKey(nameof(WorkflowId))]

        public WorkFlow Workflow { get; private set; }

        public ViolationType ViolationType { get; private set; } = ViolationType.Error;
        public string? ViolationMessage { get; private set; }
        public bool IsActive { get; private set; } = true;
        public string Expression { get; private set; }
        public List<RuleAuditLog> RuleAuditLogs { get; private set; } = new();

        [NotMapped]
        public IReadOnlyCollection<RuleAuditLog> _ruleAuditLog => RuleAuditLogs.AsReadOnly();
        public Rule()
        {

        }
        public Rule(long workflowId, string expression, string? violationMessage)
        {
            WorkflowId = workflowId;
            Expression = expression;
            ViolationMessage = violationMessage;
        }
        public static Rule CreateRuleForCustomeWorkflow(string name, long workflowId, string expression, string? violationMessage, ViolationType violationType)
        {
            var rule = new Rule();
            rule.WorkflowId = workflowId;
            rule.Expression = expression;
            rule.Name = name;
            rule.ViolationMessage = violationMessage;
            rule.ViolationType = violationType;
            return rule;
        }
        public void Update(string expression, string? violationMessage, ViolationType violationType)
        {
            Expression = expression;
            ViolationMessage = violationMessage;
            ViolationType = violationType;


        }
        public void UpdateExpression(string expression)
        {

            Expression = expression;

        }
        public void UpdateViolationMessage(string violationMessage)
        {

            ViolationMessage = violationMessage;

        }
        public void UpdateViolationType(ViolationType violationType)
        {

            ViolationType = violationType;

        }



        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;


        public void GenerateUniqueNameAsync(int count, string workflowName)
        {

            Name = $"{workflowName}-Rule-{count + 1}";
        }

        public void AddRuleAuditLog(RuleAuditLog ruleAuditLog)
        {
            if (ruleAuditLog == null) throw new ArgumentNullException(nameof(ruleAuditLog));
            RuleAuditLogs.Add(ruleAuditLog);
        }
    }
}
