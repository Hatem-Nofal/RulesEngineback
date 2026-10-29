using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{
    [Table("ActivitySubTypeDtgs", Schema = "Lookups")]
    public class ActivitySubTypeDtg : Entity<int>
    {
        public long ActivitySubTypeId { get; set; }
        [ForeignKey(nameof(ActivitySubTypeId))]
        public ActivitySubType ActivitySubType { get; set; }
        public int DuplicateTherapyGroupId { get; set; }
        [ForeignKey(nameof(DuplicateTherapyGroupId))] public DuplicateTherapyGroup DuplicateTherapyGroup { get; set; }
        public override object[] GetKeys() => new object[] { ActivitySubTypeId, DuplicateTherapyGroupId };

    }
}
