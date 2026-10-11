using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System.Collections.Generic;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;

/// <summary>
/// Turns a rule tree into an expression string for ONE kind of RuleFormConfig.
/// A config selects its strategy through RuleFormConfig.StrategyKey.
/// </summary>
public interface IRuleExpressionStrategy
{
    /// <summary>Stable key stored in RuleFormConfig.StrategyKey (never rename it once data exists).</summary>
    string Key { get; }

    string DisplayName { get; }
    /// <summary>One leaf: field + operator + value, e.g. ctx.Diagnoses.Contains("2B5D.0")</summary>
    System.Threading.Tasks.Task<string> RenderCondition(RuleNodeDto condition);

    /// <summary>
    /// Joins a group's already-rendered children with && or ||.
    /// <paramref name="leaves"/> are its direct conditions, <paramref name="subGroups"/> its nested groups
    /// (each already rendered, including its own parentheses).
    /// </summary>
    string RenderGroup(RuleNodeDto group, IReadOnlyList<string> leaves, IReadOnlyList<string> subGroups, bool isRoot);

    /// <summary>Wraps the finished tree, e.g. !( ... ).</summary>
    string Finalize(string condition);
}

