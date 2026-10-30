using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace RulesEngine.WorkFlows.Dtos
{
    public class RulePagedAndSortedResultRequestDto: PagedAndSortedResultRequestDto
    {
        public Guid? WorkflowId { get;  set; }
        public string? RuleName { get;  set; }
    }
}
