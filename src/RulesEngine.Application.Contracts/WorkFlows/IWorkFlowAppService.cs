using RulesEngine.WorkFlows.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Services;

namespace RulesEngine.WorkFlows
{
    public interface IWorkFlowAppService : ICrudAppService<WorkflowDto, WorkflowDto, Guid, WorkflowPagedAndFilterDto, SaveWorkflowDto, SaveWorkflowDto>
    {
    }
}
