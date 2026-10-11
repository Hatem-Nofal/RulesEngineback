using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.WorkFlows.Rules.RuleBuilders
{
    public interface IFieldMapProvider
    {
        Task<Dictionary<string, FieldMapDto>> GetAsync();
    }
}
