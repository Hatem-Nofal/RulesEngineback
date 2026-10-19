using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    [Table("Rules")]
    public sealed class Rule : FullAuditedEntity<Guid>
    {
        public string Name { get; private set; }

        public Guid WorkflowId { get; private  set; }

        [ForeignKey(nameof(WorkflowId))]

        public WorkFlow Workflow { get; private set; }

        public string? SuccessEvent { get; set; }=string.Empty;
        public string? ErrorMessage { get; set; } = string.Empty;
        public bool IsActive { get; private set; } = true;
        public string Expression { get; private set; }

        // EF mapping property - keep list with private setter
        public List<RuleNode> Nodes { get; private set; } = new();

        [NotMapped]
        public IReadOnlyCollection<RuleNode> _Nodes => Nodes.AsReadOnly();

        public  ICollection<RuleDiagnosis> RuleDiagnosis { get; private set; } = new List<RuleDiagnosis>();
        public  ICollection<RuleActivitySubType> RuleActivitySubTypes { get; private set; } = new List<RuleActivitySubType>();
        public  ICollection<RuleSpecialities> RuleSpecialities { get; private set; } = new List<RuleSpecialities>();

        public Rule()
        {
            
        }
        public Rule(Guid id, string name, Guid workflowId, string expression)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            WorkflowId = workflowId;
            Expression = expression;
        }
        public  void Update(string name, Guid workflowId, string expression)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            WorkflowId = workflowId;
            Expression = expression;

        }
        public  void UpdateExpression( string expression)
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
    }
}
