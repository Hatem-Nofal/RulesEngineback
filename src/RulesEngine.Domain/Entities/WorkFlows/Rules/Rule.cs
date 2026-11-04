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

    

        public ICollection<RuleDiagnosis> RuleDiagnosis { get; private set; } = new List<RuleDiagnosis>();
        public ICollection<RuleActivitySubType> RuleActivitySubTypes { get; private set; } = new List<RuleActivitySubType>();
        public ICollection<RuleSpecialities> RuleSpecialities { get; private set; } = new List<RuleSpecialities>();

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
        public void Update(long workflowId, string expression, string? violationMessage)
        {
            WorkflowId = workflowId;
            Expression = expression;
            ViolationMessage = violationMessage;

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

        public void AddNode(RuleNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Workflow.Nodes.Add(node);
        }
        public void AddNodes(List<RuleNode> nodes)
        {
            Workflow.Nodes.AddRange(nodes);
        }

        public void RemoveNode(RuleNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            Workflow.Nodes.Remove(node);
        }
        public void ResetNodes()
        {

            Workflow.Nodes.Clear();
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public void AddDiagnosis(RuleDiagnosis diagnosis)
        {
            if (diagnosis == null) throw new ArgumentNullException(nameof(diagnosis));
            RuleDiagnosis.Add(diagnosis);
        }

        public void AddActivitySubType(RuleActivitySubType activitySubType)
        {
            if (activitySubType == null) throw new ArgumentNullException(nameof(activitySubType));
            RuleActivitySubTypes.Add(activitySubType);
        }

        public void AddSpeciality(RuleSpecialities speciality)
        {
            if (speciality == null) throw new ArgumentNullException(nameof(speciality));
            RuleSpecialities.Add(speciality);
        }

        public void GenerateUniqueNameAsync(int count, string workflowName)
        {

            Name = $"{workflowName}-Rule-{count + 1}";
        }
    }
}
