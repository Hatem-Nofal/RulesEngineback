using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Lookups;

[Table("ActivityValidDates", Schema = "Lookups")]

public class ActivityValidDate : Entity<int>
{
    private ActivityValidDate() { }

    public ActivityValidDate(int id, DateTime? fromDate, DateTime? toDate, string? description = null)
    {
        Id = id;
        FromDate = fromDate;
        ToDate = toDate;
        Description = description;
    }

    public DateTime? FromDate { get; private set; }
    public DateTime? ToDate { get; private set; }
    public string? Description { get; private set; }
}
