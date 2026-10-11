using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Lookups
{

    [Table("Specialities", Schema = "Lookups")]

    public class Specialities : Entity<int>
    {

        public string Description { get; private set; }
        public string? Code { get; private set; }

        private Specialities() {}

        public Specialities(int id, string description, string code)
        {
            Id = id;
            Description = description;
            Code = code;
        }
    }
}
