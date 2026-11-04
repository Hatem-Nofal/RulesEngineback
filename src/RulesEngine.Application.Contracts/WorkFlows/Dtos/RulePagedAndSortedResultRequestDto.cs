using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.WorkFlows.Dtos
{
    public class RulePagedAndSortedResultRequestDto: PagedAndSortedResultRequestDto
    {
        public long? WorkflowId { get;  set; }
        public string? RuleName { get;  set; }
    }
}
