using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.WorkFlows.Dtos
{
    public class WorkflowPagedAndFilterDto : PagedAndSortedResultRequestDto
    {
        public string? Name { get; set; }
        public Guid? RuleFormConfigId { get; set; }
        public RejectionLevel? RejectionLevel { get; set; }

    }
}
