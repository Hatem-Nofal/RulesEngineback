using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Lookups
{

    [Table("ProcedureGroup", Schema = "Lookups")]
    public class ProcedureGroup : Entity<int>
    {
        public string ProcedureGroupSystemCode { get; set; }

        public string ProcedureGroupName { get; set; }

        public bool IsActive { get; set; } = true;

    }
}

