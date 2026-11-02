using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.Logers.Dtos
{
    public class RulesEngineExecutionLogFiltersDto: PagedAndSortedResultRequestDto
    {
        public string? WorkFlowName { get; set; }
        public string? RuleName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Activity { get; set; }
        public string? ClaimNumber { get; set; }
    }
}
