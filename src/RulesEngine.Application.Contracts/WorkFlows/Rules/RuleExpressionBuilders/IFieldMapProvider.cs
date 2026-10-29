using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders
{
    public interface IFieldMapProvider
    {
        Task<Dictionary<string, FieldMapDto>> GetAsync();
    }
}
