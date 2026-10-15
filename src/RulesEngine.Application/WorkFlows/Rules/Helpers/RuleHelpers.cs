using RulesEngine.Common.Consts;
using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RulesEngine.WorkFlows.Rules.Helpers
{
    // Minimal placeholders so application compiles. Replace with real implementations.


    public class ExpressionBuildResult
    {
        public string Expression { get; set; } = string.Empty;
    }

    public static class RuleTreeMapper
    {
        public static RuleNodeDto ToTree(List<RuleNode> nodes)
        {
            return new RuleNodeDto();
        }

        public static void Flatten(RuleNodeDto root, Guid ruleId, Guid? parentId, int order, List<RuleNode> flat)
        {
            // noop placeholder
        }
    }

    public static class RuleTreeValidator
    {
        private static readonly Dictionary<string, RuleOperator[]> DefaultOperators = new()
        {
            [DataTypes.Number] = new[]
      {
            RuleOperator.Equal, RuleOperator.NotEqual, RuleOperator.GreaterThan,
            RuleOperator.GreaterThanOrEqual, RuleOperator.LessThan, RuleOperator.LessThanOrEqual
        },
            [DataTypes.String] = new[] { RuleOperator.Equal, RuleOperator.NotEqual },
            [DataTypes.Enum] = new[] { RuleOperator.Equal, RuleOperator.NotEqual },
            [DataTypes.List] = new[] { RuleOperator.Equal, RuleOperator.NotEqual, RuleOperator.Contains, RuleOperator.NotContains },
        };

        /// <summary>All operators valid for a field data type (used when a config doesn't restrict them).</summary>
        public static RuleOperator[] OperatorsFor(string dataType) =>
            DefaultOperators.TryGetValue(dataType, out var d) ? d : Array.Empty<RuleOperator>();

        public static void Validate(RuleNodeDto root, RuleFormConfigDto config)
        {
            if (root is null || root.NodeType != RuleNodeType.Group)
                throw new RuleValidationException("The rule must start with a group.");

            var allowed = config.Fields.ToDictionary(f => f.Key);
            Walk(root, allowed, config.AllowNestedGroups, isRoot: true);
        }

        //private static void Walk(RuleNodeDto n, Dictionary<string, RuleFormFieldDto> allowed, bool allowNested, bool isRoot)
        //{
        //    if (n.NodeType == RuleNodeType.Group)
        //    {
        //        if (n.LogicalOperator is null)
        //            throw new RuleValidationException("Each group needs AND / OR.");
        //        if (n.Children.Count == 0)
        //            throw new RuleValidationException("A group cannot be empty.");
        //        if (!allowNested && !isRoot)
        //            throw new RuleValidationException("Nested groups are not allowed for this workflow.");

        //        foreach (var c in n.Children) Walk(c, allowed, allowNested, isRoot: false);
        //        return;
        //    }

        //    if (n.FieldKey is null || !allowed.TryGetValue(n.FieldKey, out var field))
        //        throw new RuleValidationException($"Field '{n.FieldKey}' is not allowed for this workflow.");

        //    if (n.Operator is null)
        //        throw new RuleValidationException($"Choose an operator for '{field.Label}'.");

        //    RuleOperator[] ops;
        //    if (field.AllowedOperators is { Count: > 0 })
        //        ops = field.AllowedOperators.Select(x => (RuleOperator)x).ToArray();
        //    else
        //        ops = DefaultOperators.TryGetValue(field.DataType, out var d) ? d : Array.Empty<RuleOperator>();

        //    if (!ops.Contains(n.Operator.Value))
        //        throw new RuleValidationException($"Operator not allowed for '{field.Label}'.");

        //    if (!n.Value.HasValue
        //        || n.Value.Value.ValueKind == JsonValueKind.Null
        //        || n.Value.Value.ValueKind == JsonValueKind.Undefined)
        //        throw new RuleValidationException($"Enter a value for '{field.Label}'.");

        //    var value = n.Value.Value;
        //    var isList = n.Operator == RuleOperator.Contains || n.Operator == RuleOperator.NotContains;

        //    if (isList && (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0))
        //        throw new RuleValidationException($"Select at least one value for '{field.Label}'.");
        //    if (!isList && value.ValueKind == JsonValueKind.Array)
        //        throw new RuleValidationException($"'{field.Label}' expects a single value.");
        //}

        private static void Walk(RuleNodeDto n, Dictionary<string, RuleFormFieldDto> allowed, bool allowNested, bool isRoot)
        {
            if (n.NodeType == RuleNodeType.Group)
            {
                if (n.Children.Count == 0)
                    throw new RuleValidationException("A group cannot be empty.");
                if (!allowNested && !isRoot)
                    throw new RuleValidationException("Nested groups are not allowed for this workflow.");

                for (var i = 0; i < n.Children.Count; i++)
                {
                    if (i > 0 && n.Children[i].LogicalJoinOperator is null)
                        throw new RuleValidationException("Choose AND / OR before every item after the first.");
                    Walk(n.Children[i], allowed, allowNested, isRoot: false);
                }
                return;
            }

            if (n.FieldKey is null || !allowed.TryGetValue(n.FieldKey, out var field))
                throw new RuleValidationException($"Field '{n.FieldKey}' is not allowed for this workflow.");

            if (n.Operator is null)
                throw new RuleValidationException($"Choose an operator for '{field.Label}'.");

            RuleOperator[] ops;
            if (field.AllowedOperators is { Count: > 0 })
                ops = field.AllowedOperators.Select(x => (RuleOperator)x).ToArray();
            else
                ops = DefaultOperators.TryGetValue(field.DataType, out var d) ? d : Array.Empty<RuleOperator>();

            if (!ops.Contains(n.Operator.Value))
                throw new RuleValidationException($"Operator not allowed for '{field.Label}'.");

            if (!n.Value.HasValue
                || n.Value.Value.ValueKind == JsonValueKind.Null
                || n.Value.Value.ValueKind == JsonValueKind.Undefined)
                throw new RuleValidationException($"Enter a value for '{field.Label}'.");

            var value = n.Value.Value;
            var op = n.Operator.Value;
            var isList = value.ValueKind == JsonValueKind.Array&&(n.Operator == RuleOperator.Contains || n.Operator == RuleOperator.NotContains || n.Operator == RuleOperator.Equal || n.Operator == RuleOperator.NotEqual);


            if (isList && (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0))
                throw new RuleValidationException($"Select at least one value for '{field.Label}'.");
            // Collection operators take one value or several (any of / all of / none of)
            if (isList && field.IsMultiple && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0)
                throw new RuleValidationException($"Select at least one value for '{field.Label}'.");
            if (isList && !field.IsMultiple && value.ValueKind == JsonValueKind.Array)
                throw new RuleValidationException($"'{field.Label}' expects a single value.");
        }
    }
}
