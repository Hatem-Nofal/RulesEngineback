
using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.Helpers;
using RulesEngine.WorkFlows.Rules.RuleBuilders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;

public record RuleExpressionResult(string StrategyKey, string Condition, string Expression);

/// <summary>
/// Walks the rule tree once (shared logic) and delegates every leaf/group/final wrap to the
/// strategy chosen by the workflow's form config.
/// </summary>
public class RuleExpressionBuilder
{
    private readonly IRuleExpressionStrategy _strategy;
    private readonly IRuleFormConfigAppService _forms;

    public RuleExpressionBuilder(IRuleExpressionStrategy strategy, IRuleFormConfigAppService forms)
    {
        _strategy = strategy;
        _forms = forms;
    }

    /// <summary>Validates the tree against the workflow's config, then builds the expression.</summary>
    public async Task<RuleExpressionResult> BuildForRuleAsync(RuleDto rule)
    {
        var config = await _forms.GetByWorkflowAsync(rule.WorkflowId)
                     ?? throw new RuleValidationException("This workflow has no form config assigned.");
        RuleTreeValidator.Validate(rule.Root, config);
        return await Build(rule.Root, config.StrategyKey);
    }

    public async Task<RuleExpressionResult> Build(RuleNodeDto root, string? strategyKey)
    {
        var condition = await Render(root, _strategy, isRoot: true);
        return new RuleExpressionResult(_strategy.Key, condition, _strategy.Finalize(condition));
    }

    private static async Task<string> Render(RuleNodeDto node, IRuleExpressionStrategy strategy, bool isRoot)
    {
        if (node.NodeType == RuleNodeType.Condition)
            return await strategy.RenderCondition(node);

        var leaves = new List<string>();
        foreach (var c in node.Children.Where(c => c.NodeType == RuleNodeType.Condition))
            leaves.Add(await Render(c, strategy, isRoot: false));

        var subGroups = new List<string>();
        foreach (var c in node.Children.Where(c => c.NodeType == RuleNodeType.Group))
            subGroups.Add(await Render(c, strategy, isRoot: false));

        return strategy.RenderGroup(node, leaves, subGroups, isRoot);
    }
}
