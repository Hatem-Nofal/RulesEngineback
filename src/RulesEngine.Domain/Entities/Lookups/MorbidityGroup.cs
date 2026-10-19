using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Lookups
{
    [Table("MorbidityGroups", Schema = "Lookups")]
    public class MorbidityGroup : Entity<int>
    {
        [Required]
        [StringLength(30)]
        public string MorbidityGroupSystemCode { get; set; }

        [Required]
        [StringLength(1000)]
        public string MorbidityGroupName { get; set; }

        public bool IsActive { get; set; } = true;


    }
}
