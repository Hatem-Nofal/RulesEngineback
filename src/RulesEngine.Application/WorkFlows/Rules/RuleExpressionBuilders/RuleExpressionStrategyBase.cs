using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Rules.RuleBuilders;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;



 

/// <summary>
/// Shared rendering. A concrete strategy only declares its field map (and may override any 
/// member when a config needs different syntax or composition).
/// </summary>
public sealed class RuleExpressionStrategyBase : IRuleExpressionStrategy
{
    public string Key { get; } = "default";
    public string DisplayName { get; } = "Default Strategy";
    private readonly IFieldMapProvider _maps;

    public RuleExpressionStrategyBase(IFieldMapProvider maps)
    {
        _maps = maps;
    }


    public  string RenderCondition(RuleNodeDto node, FieldMapDto field)
    {
        if (node.Operator is null)
            throw new RuleValidationException($"Missing operator for '{node.FieldKey}'.");
        if (!node.Value.HasValue)
            throw new RuleValidationException($"Missing value for '{node.FieldKey}'.");

        var op = node.Operator.Value;
        var value = node.Value.Value;

        
            return field.Kind switch
            {
                FieldKind.List when node.IsMultiple => RenderMultipleSelect(field, op, value),
                FieldKind.List => RenderCollection(field, op, value),
                FieldKind.Number => RenderNumber(field, op, value),
                FieldKind.Text => RenderText(field, op, value),
                FieldKind.Decimal => RenderDecimal(field, op, value),
                FieldKind.DateTime => RenderDateTime(field, op, value),
                _ => throw new RuleValidationException($"Unsupported field kind for '{node.FieldKey}'.")
            };
    
    }

    //public  async System.Threading.Tasks.Task<string> RenderCondition(RuleNodeDto node)
    //{
    //    var fields = await _maps.GetAsync();
    //    if (node.FieldKey is null || !fields.TryGetValue(node.FieldKey, out var field))
    //        throw new RuleValidationException(
    //            $"Field '{node.FieldKey}' is not supported by the '{Key}' expression strategy.");
    //    if (node.Operator is null)
    //        throw new RuleValidationException($"Missing operator for '{node.FieldKey}'.");
    //    if (!node.Value.HasValue)
    //        throw new RuleValidationException($"Missing value for '{node.FieldKey}'.");

    //    var op = node.Operator.Value;
    //    var value = node.Value.Value;

    //    return field.Kind switch
    //    {
    //        FieldKind.List when node.IsMultiple => RenderMultipleSelect(field, op, value),
    //        FieldKind.List => RenderCollection(field, op, value),
    //        FieldKind.Number => RenderNumber(field, op, value),
    //        FieldKind.Text => RenderText(field, op, value),
    //        FieldKind.Decimal => RenderDecimal(field, op, value),
    //        FieldKind.DateTime => RenderDateTime(field, op, value),
    //        _ => throw new RuleValidationException($"Unsupported field kind for '{node.FieldKey}'.")
    //    };
    //}

    /// <summary>
    /// Rules:
    ///  - every nested group is wrapped in ( ... ), even with a single condition;
    ///  - when a group has BOTH direct conditions and nested groups, its conditions form one ( ... ) block
    ///    that comes first, then each nested group, all joined with the group's own &amp;&amp; / ||;
    ///  - the root group is not wrapped here (Finalize wraps it).
    /// Regrouping conditions that share the group's operator never changes the logical result.
    /// </summary>
    //public  string RenderGroup(RuleNodeDto group, IReadOnlyList<string> leaves,
    //    IReadOnlyList<string> subGroups, bool isRoot)
    //{
    //    var glue = group.LogicalOperator == LogicalOperator.Or ? " || " : " && ";

    //    var parts = new List<string>();
    //    if (leaves.Count > 0)
    //    {
    //        var block = string.Join(glue, leaves);
    //        parts.Add(subGroups.Count > 0 ? $"({block})" : block);
    //    }
    //    parts.AddRange(subGroups);

    //    var joined = string.Join(glue, parts);
    //    return isRoot ? joined : $"({joined})";
    //}
    public  string RenderGroup(RuleNodeDto group, IReadOnlyList<RenderedItem> items, bool isRoot)
    {
        var runs = new List<List<string>>();
        for (var i = 0; i < items.Count; i++)
        {
            if (i == 0 || items[i].Join == LogicalOperator.Or)
                runs.Add(new List<string>());
            runs[^1].Add(items[i].Text);
        }

        var parts = runs.Select(r =>
            r.Count == 1 ? r[0]
            : runs.Count > 1 ? $"({string.Join(" && ", r)})"
            : string.Join(" && ", r)).ToList();

        var joined = string.Join(" || ", parts);
        return isRoot ? joined : $"({joined})";
    }

    /// <summary>
    /// The tree describes when the item is BLOCKED, so the validity expression is its negation.
    /// Override in a strategy that wants the raw condition instead.
    /// </summary>
    public  string Finalize(string condition) => $"!({condition})";

    // ----- per-kind rendering -----

    protected  string RenderCollection(FieldMapDto f, RuleOperator op, JsonElement v)
    {
         return op switch
        {
            RuleOperator.Equal => $"{f.key}=={Str(v)}",
            RuleOperator.NotEqual => $"{f.key}!={Str(v)}",
            _ => throw Unsupported(f, op)
        };
    }
    protected  string RenderMultipleSelect(FieldMapDto f, RuleOperator op, JsonElement v)
    {
        var items = Strings(v);
        var parts = items.Select(s => $"{f.key}.Contains({Quote(s)})").ToList();
        var expr = parts.Count == 1 ? parts[0] : $"({string.Join(" || ", parts)})";
        return op switch
        {
            RuleOperator.Contains => expr,
            RuleOperator.NotContains => $"!({expr})",
            _ => throw Unsupported(f, op)
        };
    }


    protected  string RenderNumber(FieldMapDto f, RuleOperator op, JsonElement v)
    {
        var symbol = op switch
        {
            RuleOperator.Equal => "==",
            RuleOperator.NotEqual => "!=",
            RuleOperator.GreaterThan => ">",
            RuleOperator.GreaterThanOrEqual => ">=",
            RuleOperator.LessThan => "<",
            RuleOperator.LessThanOrEqual => "<=",
            _ => throw Unsupported(f, op)
        };
        return $"{f.key} {symbol} {Num(v)}";
    }
    protected  string RenderDecimal(FieldMapDto f, RuleOperator op, JsonElement v)
    {
        var symbol = op switch
        {
            RuleOperator.Equal => "==",
            RuleOperator.NotEqual => "!=",
            RuleOperator.GreaterThan => ">",
            RuleOperator.GreaterThanOrEqual => ">=",
            RuleOperator.LessThan => "<",
            RuleOperator.LessThanOrEqual => "<=",
            _ => throw Unsupported(f, op)
        };
        return $"{f.key} {symbol} {Decimal(v)}";
    }
    protected string RenderDateTime(FieldMapDto f, RuleOperator op, JsonElement v)
    {
        var symbol = op switch
        {
            RuleOperator.Equal => "==",
            RuleOperator.NotEqual => "!=",
            RuleOperator.GreaterThan => ">",
            RuleOperator.GreaterThanOrEqual => ">=",
            RuleOperator.LessThan => "<",
            RuleOperator.LessThanOrEqual => "<=",
            _ => throw Unsupported(f, op)
        };

        return $"{f.key} {symbol} {GetDateTime(v)}";
    }

    protected  string RenderText(FieldMapDto f, RuleOperator op, JsonElement v)
    {
        switch (op)
        {
            case RuleOperator.Equal:
                return $"{f.key} == {Str(v)}";
            case RuleOperator.NotEqual:
                return $"{f.key} != {Str(v)}";
            case RuleOperator.Contains:
            {
                var parts = Strings(v).Select(s => $"{f.key} == {Quote(s)}").ToList();
                return parts.Count == 1 ? parts[0] : $"({string.Join(" || ", parts)})";
            }
            case RuleOperator.NotContains:
            {
                var parts = Strings(v).Select(s => $"{f.key} == {Quote(s)}").ToList();
                return $"!({string.Join(" || ", parts)})";
            }
            default:
                throw Unsupported(f, op);
        }
    }

    // ----- literals -----

    protected static string Quote(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

    protected static string Str(JsonElement v) =>
        v.ValueKind == JsonValueKind.String
            ? Quote(v.GetString()!)
            : throw new RuleValidationException("Expected a text value.");
    protected static string List(JsonElement v) =>
       v.ValueKind == JsonValueKind.Array
           ? string.Join(", ", v.EnumerateArray().Select(e => Quote(e.GetString()!)))
           : throw new RuleValidationException("Expected an array value.");

    protected static string Num(JsonElement v) =>
        v.ValueKind == JsonValueKind.Number
            ? v.GetInt32().ToString(CultureInfo.InvariantCulture)
            : throw new RuleValidationException("Expected a numeric value.");
    protected static string Decimal(JsonElement v) =>
        v.ValueKind == JsonValueKind.Number
            ? v.GetDecimal().ToString(CultureInfo.InvariantCulture)
            : throw new RuleValidationException("Expected a numeric value.");

    protected static string GetDateTime(JsonElement v)
    {
        if (v.ValueKind != JsonValueKind.String)
            throw new RuleValidationException("Expected a date/time value.");

        var value = v.GetString();

        if (!System.DateTime.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var date))
        {
            throw new RuleValidationException("Invalid date/time value.");
        }

        return $"DateTime.Parse({Quote(date.ToString(
            "O",
            System.Globalization.CultureInfo.InvariantCulture))})";
    }

    protected static List<string> Strings(JsonElement v)
    {
        var items = v.ValueKind == JsonValueKind.Array ? v.EnumerateArray().ToList() : new List<JsonElement> { v };
        if (items.Count == 0 || items.Any(i => i.ValueKind != JsonValueKind.String))
            throw new RuleValidationException("Expected one or more text values.");
        return items.Select(i => i.GetString()!).ToList();
    }

    protected RuleValidationException Unsupported(FieldMapDto f, RuleOperator op) =>
        new($"Operator '{op}' is not supported for '{f.key}' by the '{Key}' strategy.");
}
