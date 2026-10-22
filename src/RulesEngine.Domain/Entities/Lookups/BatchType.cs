using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{
    [Table("BatchTypes", Schema = "Lookups")]
    public class BatchType : Entity<int>
    {
        [Required][StringLength(10)] public string Code { get; set; }
        [Required][StringLength(100)] public string Name { get; set; }

    }
}
