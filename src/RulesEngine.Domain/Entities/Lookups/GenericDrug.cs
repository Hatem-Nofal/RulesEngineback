using Volo.Abp.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Lookups
{
    [Table("GenericDrugs", Schema = "Lookups")]
    public class GenericDrug : FullAuditedEntity<int>
    {
        [Required]
        public string SystemCode { get; set; }
        [Required]
        [MaxLength(1000)]
        public string GenericCode { get; set; }
        [Required]
        [MaxLength(1500)]
        public string GenericName { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey("CreatorId")]
        public virtual IdentityUser? CreatorBy { get; set; }
        [ForeignKey("LastModifierId")]
        public virtual IdentityUser? LastModifiedBy { get; set; }
    }
}