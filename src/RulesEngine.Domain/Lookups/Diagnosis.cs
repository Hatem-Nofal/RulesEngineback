using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Lookups
{
    [Table("Diagnoses", Schema = "Lookups")]
    public class Diagnosis : Entity<long>
    {
        public string IqviaCode { get; private set; }
        public string ShortDesc { get; private set; }
        public string MediumDesc { get; private set; }
        public int Status { get; private set; }

        public string IqviaType { get; private set; }

        public DateTime Create_timestamp { get; private set; }

        public DateTime Update_timestamp { get; private set; }

        private Diagnosis() { }

        public Diagnosis(long id, string iqviaCode, string shortDesc, int status)
        {
            Id = id;
            IqviaCode = iqviaCode;
            ShortDesc = shortDesc;
            Status = status;
            Create_timestamp = DateTime.UtcNow;
            Update_timestamp = Create_timestamp;
        }

        public void UpdateDescription(string shortDesc, string mediumDesc)
        {
            ShortDesc = shortDesc;
            MediumDesc = mediumDesc;
            Update_timestamp = DateTime.UtcNow;
        }
    }
}
