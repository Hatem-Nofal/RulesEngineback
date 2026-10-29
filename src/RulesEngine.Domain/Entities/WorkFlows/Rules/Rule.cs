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
    public sealed class Rule : FullAuditedEntity<Guid>
    {
        public string Name { get; private set; }

        public Guid WorkflowId { get; private set; }

        [ForeignKey(nameof(WorkflowId))]

        public WorkFlow Workflow { get; private set; }

        public ViolationType ViolationType { get; private set; } = ViolationType.Error;
        public string? ViolationMessage { get; private set; } 
        public bool IsActive { get; private set; } = true;
        public string Expression { get; private set; }

        // EF mapping property - keep list with private setter
        public List<RuleNode> Nodes { get; private set; } = new();

        [NotMapped]
        public IReadOnlyCollection<RuleNode> _Nodes => Nodes.AsReadOnly();

        public ICollection<RuleDiagnosis> RuleDiagnosis { get; private set; } = new List<RuleDiagnosis>();
        public ICollection<RuleActivitySubType> RuleActivitySubTypes { get; private set; } = new List<RuleActivitySubType>();
        public ICollection<RuleSpecialities> RuleSpecialities { get; private set; } = new List<RuleSpecialities>();

        public Rule()
        {

        }
        public Rule(Guid id, Guid workflowId, string expression, string? violationMessage)
        {
            Id = id;
            WorkflowId = workflowId;
            Expression = expression;
            ViolationMessage = violationMessage;
        }
        public static Rule CreateRuleForCustomeWorkflow( Guid id, string name, Guid workflowId, string expression)
        {
            var rule = new Rule();
            rule.Id = id;
            rule.WorkflowId = workflowId;
            rule.Expression = expression;
            rule.Name = name;
            return rule;
        }
        public void Update(Guid workflowId, string expression, string? violationMessage)
        {
            WorkflowId = workflowId;
            Expression = expression;
            ViolationMessage = violationMessage;

        }
        public void UpdateExpression(string expression)
        {

            Expression = expression;

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

            Nodes = new();
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

        public void GenerateUniqueNameAsync(int count,string workflowName)
        {

            Name= $"{workflowName}-Rule-{count + 1}";
        }
    }
}
