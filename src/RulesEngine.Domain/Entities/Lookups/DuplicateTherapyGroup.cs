using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{
    [Table("DuplicateTherapyGroups", Schema = "Lookups")]
    public class DuplicateTherapyGroup : Entity<int>
    {
        [Required]
        [StringLength(20)]
        public string DtgSystemCode { get; set; }

        [Required]
        [StringLength(1000)]
        public string DtGroupName { get; set; }

    }
}
