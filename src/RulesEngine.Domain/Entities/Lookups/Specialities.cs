using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{

    [Table("Specialities", Schema = "Lookups")]

    public class Specialities : Entity<int>
    {

        public string SpecialityCode { get; set; }
        public string? SpecialityDisplay { get; set; }
        public string? SpecialityDescription { get; set; }
        public string? Group { get; set; }

        private Specialities() {}

        public Specialities(int id, string description, string code, string? group)
        {
            Id = id;
            SpecialityDescription = description;
            SpecialityCode = code;
            Group = group;
        }
    }
}
