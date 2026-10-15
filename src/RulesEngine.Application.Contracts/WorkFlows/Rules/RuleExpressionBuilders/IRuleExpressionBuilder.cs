using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders
{
    public interface IRuleExpressionBuilder
    {
        Task<RuleExpressionResultDto> BuildForRuleAsync(RuleDto rule);
        Task<RuleExpressionResultDto> Build(RuleNodeDto root, string? strategyKey, IReadOnlyDictionary<string, FieldMapDto> fields);
    }
}

