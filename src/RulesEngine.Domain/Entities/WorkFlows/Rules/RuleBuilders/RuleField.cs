using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleFields", Schema = "RuleBuilders")]
    public class RuleField : Entity<Guid>
    {
        public string Key { get; private set; }
        public string Label { get; private set; }
        public string DataType { get; private set; }
        public string? OptionsJson { get; private set; }

        private RuleField() { }

        public RuleField(Guid id, string key, string label, string dataType, string? optionsJson = null)
        {
            Id = id;
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            OptionsJson = optionsJson;
        }
    }
}
