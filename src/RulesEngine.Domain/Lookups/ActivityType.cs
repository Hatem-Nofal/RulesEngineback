using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Lookups
{

    [Table("Activity_types", Schema = "Lookups")]
    public class ActivityType : Entity<int>
    {
        [Required]
        [StringLength(30)]
        public string Description { get; private set; }

        [Required]
        public DateTime Create_timestamp { get; private set; }

        [Required]
        public DateTime Update_timestamp { get; private set; }

        public bool IsActive { get; private set; }

        private ActivityType() { }

        public ActivityType(int id, string description, bool isActive = true)
        {
            Id = id;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Create_timestamp = DateTime.UtcNow;
            Update_timestamp = Create_timestamp;
            IsActive = isActive;
        }

        public void UpdateDescription(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Update_timestamp = DateTime.UtcNow;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            Update_timestamp = DateTime.UtcNow;
        }
    }
}
