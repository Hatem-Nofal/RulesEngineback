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

        /// <summary>
        /// Same as <see cref="BuildForRuleAsync(RuleDto)"/> but for callers that already hold the workflow's form
        /// config (bulk import), so no database lookup happens per rule.
        /// </summary>
        Task<RuleExpressionResultDto> BuildForRuleAsync(RuleDto rule, RuleFormConfigDto config);
        Task<RuleExpressionResultDto> Build(RuleNodeDto root, string? strategyKey, IReadOnlyDictionary<string, FieldMapDto> fields);

        /// <summary>
        /// Reverse of <see cref="Build(RuleNodeDto, string?, IReadOnlyDictionary{string, FieldMapDto})"/>.
        /// Parses the generated expression back into a rule tree using the provided workflow fields.
        /// </summary>
        Task<RuleNodeDto> ReverseBuild(string expression, IReadOnlyList<RuleFormFieldDto> fields);
        Task<RuleFormConfigDto> LoadFormConfigAsync(long workflowId);
    }
}

