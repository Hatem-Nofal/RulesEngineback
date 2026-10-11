using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Lookups
{
    [Table("Activity_sub_types", Schema = "Lookups")]
    public class ActivitySubType : Entity<long>
    {
        public string IqviaCode { get; private set; }
        public string ShortDesc { get; private set; }
        public string MediumDesc { get; private set; }
        public int ActivityId { get; private set; }

        [ForeignKey("Activity_id")]
        public ActivityType ActivityType { get; private set; }
        public int Status { get; private set; }
        public string IqviaType { get; private set; }
        public DateTime Update_timestamp { get; private set; }
        public DateTime Create_timestamp { get; private set; }

        [RegularExpression(@"^\d+(\.\d{1,3})?$", ErrorMessage = "Only up to 3 decimal places are allowed.")]
        public decimal? Price { get; private set; }

        [ForeignKey("Valid_DateId")]
        public ActivityValidDate? ActivityValidDate { get; private set; }

        public int Valid_DateId { get; private set; }

        private ActivitySubType() { }

        public ActivitySubType(long id, string iqviaCode, string shortDesc, int activityId, int status)
        {
            Id = id;
            IqviaCode = iqviaCode;
            ShortDesc = shortDesc;
            ActivityId = activityId;
            Status = status;
            Create_timestamp = DateTime.UtcNow;
            Update_timestamp = Create_timestamp;
        }

        public void UpdatePrice(decimal? price)
        {
            Price = price;
            Update_timestamp = DateTime.UtcNow;
        }

        public void SetActivityValidDate(ActivityValidDate validDate)
        {
            ActivityValidDate = validDate;
            Valid_DateId = validDate?.Id ?? 0;
            Update_timestamp = DateTime.UtcNow;
        }
    }
}
