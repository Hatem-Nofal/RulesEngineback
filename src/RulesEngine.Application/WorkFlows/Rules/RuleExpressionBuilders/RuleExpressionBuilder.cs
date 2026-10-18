
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


/// <summary>
/// Walks the rule tree once (shared logic) and delegates every leaf/group/final wrap to the
/// strategy chosen by the workflow's form config.
/// </summary>
public class RuleExpressionBuilder : IRuleExpressionBuilder
{
    private readonly IRuleExpressionStrategy _strategy;
    private readonly IRuleFormConfigAppService _forms;

    public RuleExpressionBuilder(IRuleExpressionStrategy strategy, IRuleFormConfigAppService forms)
    {
        _strategy = strategy;
        _forms = forms;
    }


    public async Task<RuleExpressionResultDto> BuildForRuleAsync(RuleDto rule)
    {
        var config = await _forms.GetByWorkflowAsync(rule.WorkflowId)
                    ?? throw new RuleValidationException("This workflow has no form config assigned.");
        return await BuildForRuleAsync(rule, config);
    }

    public async Task<RuleExpressionResultDto> BuildForRuleAsync(RuleDto rule, RuleFormConfigDto config)
    {
        RuleTreeValidator.Validate(rule.Root, config);

        var fields = config.Fields.ToDictionary(
            f => f.Key,
            f => new FieldMapDto(
                f.Key ?? throw new RuleValidationException(
                    $"Field '{f.Label}' has no context path in form config '{config.Name}'."),
                FieldMapDto.KindOf(f.DataType)));

        return await Build(rule.Root, config.StrategyKey, fields);
    }

    public async Task<RuleExpressionResultDto> Build(RuleNodeDto root, string? strategyKey, IReadOnlyDictionary<string, FieldMapDto> fields)
    {
        //  var strategy = factory.Get(strategyKey);
        var condition = Render(root, _strategy, fields, isRoot: true);
        return new RuleExpressionResultDto(_strategy.Key, condition, _strategy.Finalize(condition));
    }

    private static string Render(RuleNodeDto node, IRuleExpressionStrategy strategy,
        IReadOnlyDictionary<string, FieldMapDto> fields, bool isRoot)
    {
        if (node.NodeType == RuleNodeType.Condition)
        {
            if (node.FieldKey is null || !fields.TryGetValue(node.FieldKey, out var map))
                throw new RuleValidationException($"Field '{node.FieldKey}' is not part of this form config.");
            return strategy.RenderCondition(node, map);
        }

        // Keep the builder's order: every item carries its own AND / OR to the previous item.
        var items = node.Children.Select((c, i) => new RenderedItem(
            Render(c, strategy, fields, isRoot: false),
            c.NodeType == RuleNodeType.Group,
            i == 0 ? LogicalOperator.And : c.LogicalJoinOperator ?? LogicalOperator.And)).ToList();

        return strategy.RenderGroup(node, items, isRoot);
    }





}
