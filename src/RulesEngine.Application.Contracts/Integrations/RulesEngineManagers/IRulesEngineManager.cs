using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RulesEngine.Integrations;

namespace RulesEngine.Integrations.RulesEngineManagers
{
    public interface IRulesEngineManager
    {
        Task<RulesEngine> GetRulesEngineAsync();
        Task ReloadWorkflowsAsync();
        int GetBatchCount(string baseWorkflowName);
    }
}
