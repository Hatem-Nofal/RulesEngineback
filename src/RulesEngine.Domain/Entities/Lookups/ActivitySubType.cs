using RulesEngine.Entities.Lookups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;

namespace RulesEngine.Lookups
{
    [Table("Activity_sub_types", Schema = "Lookups")]
    public class ActivitySubType : Entity<long>
    {
        public string iqvia_code { get; set; }
        public string? Short_Desc { get; set; }
        public string? Medium_Desc { get; set; }
        public int Activity_id { get; set; }
        [ForeignKey(nameof(Activity_id))] public ActivityType ActivityType { get; set; }
        public int Status { get; set; }
        public int OccurrenceCount { get; set; }
        public string? iqvia_type { get; set; }
        public DateTime Update_timestamp { get; set; }
        public DateTime Create_timestamp { get; set; }
        public Guid? AbpCreatedBy_Id { get; set; }
        [ForeignKey("AbpCreatedBy_Id")] public virtual IdentityUser? Createdby { get; set; }
        [RegularExpression(@"^\d+(\.\d{1,3})?$", ErrorMessage = "Only up to 3 decimal places are allowed.")] public decimal? Price { get; set; }
        public int Valid_DateId { get; set; }


        [StringLength(100)] public string? SystemCode { get; set; }
        [StringLength(9)] public string? UhiaId { get; set; }
        public int? ItemSubCategoryId { get; set; }

        public Guid? LastUpdatedBy_Id { get; set; }
        [ForeignKey(nameof(LastUpdatedBy_Id))] public virtual IdentityUser? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsCapitation { get; set; } = false;
        public bool IsPreAuthorization { get; set; } = false;
        public bool IsProtocol { get; set; } = false;
        public int? GenericDrugId { get; set; }
        [ForeignKey(nameof(GenericDrugId))]
        public GenericDrug? GenericDrug { get; set; }
        public int? ProcedureGroupId { get; set; }
        [ForeignKey(nameof(ProcedureGroupId))] 
        public ProcedureGroup? ProcedureGroup { get; set; }
        public List<ActivitySubTypeDtg>? DuplicateTherapyGroups { get; set; } = new();

        private ActivitySubType() { }

        public ActivitySubType(long id, string iqviaCode, string shortDesc, int activityId, int status)
        {
            Id = id;
            iqvia_code = iqviaCode;
            Short_Desc = shortDesc;
            Activity_id = activityId;
            Status = status;
            Create_timestamp = DateTime.UtcNow;
            Update_timestamp = Create_timestamp;
        }

        public void UpdatePrice(decimal? price)
        {
            Price = price;
            Update_timestamp = DateTime.UtcNow;
        }


    }

}
