using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.Lookups;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.Helpers;
using RulesEngine.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;

/// <summary>
/// Walks the rule tree once (shared logic) and delegates every leaf/group/final wrap to the
/// strategy chosen by the workflow's form config.
/// </summary>
public class RuleExpressionBuilder : IRuleExpressionBuilder
{
    private readonly IRuleExpressionStrategy _strategy;
    private readonly IRuleFormConfigAppService _forms;
    private readonly IWorkFlowRepository _workflowRepository;
    private readonly IRepository<RuleField, long> _ruleFieldRepository;
    private readonly IRepository<ActivitySubType, long> _activitySubTypeRepository;

    public RuleExpressionBuilder(IRuleExpressionStrategy strategy, IRuleFormConfigAppService forms, IWorkFlowRepository workflowRepository, IRepository<RuleField, long> ruleFieldRepository, IRepository<ActivitySubType, long> activitySubTypeRepository)
    {
        _strategy = strategy;
        _forms = forms;
        _workflowRepository = workflowRepository;
        _ruleFieldRepository = ruleFieldRepository;
        _activitySubTypeRepository = activitySubTypeRepository;
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

        var fields = config.Fields
            .GroupBy(f => f.Key)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var f = g.Last();
                    return new FieldMapDto(
                        f.Key ?? throw new RuleValidationException(
                            $"Field '{f.Label}' has no context path in form config '{config.Name}'."),
                        FieldMapDto.KindOf(f.DataType));
                });

        return await Build(rule.Root, config.StrategyKey, fields);
    }
    public async Task<RuleExpressionResultDto> BuildForBulkRuleAsync(RuleDto rule, RuleFormConfigDto config)
    {
        RuleTreeValidator.ValidateWitErrors(rule.Root, config);

        var fields = config.Fields
            .GroupBy(f => f.Key)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var f = g.Last();
                    return new FieldMapDto(
                        f.Key ?? throw new RuleValidationException(
                            $"Field '{f.Label}' has no context path in form config '{config.Name}'."),
                        FieldMapDto.KindOf(f.DataType));
                });

        return await Build(rule.Root, config.StrategyKey, fields);
    }

    public async Task<RuleExpressionResultDto> Build(RuleNodeDto root, string? strategyKey, IReadOnlyDictionary<string, FieldMapDto> fields)
    {
        var condition = Render(root, _strategy, fields, isRoot: true);
        return new RuleExpressionResultDto(_strategy.Key, condition, _strategy.Finalize(condition));
    }

    public async Task<RuleNodeDto> ReverseBuild(string expression, IReadOnlyList<RuleFormFieldDto> fields)
    {
        var normalized = StripOuterFinalization(expression ?? string.Empty);

        if (string.IsNullOrWhiteSpace(normalized))
            throw new RuleValidationException("Expression cannot be empty.");

        var lookup = fields
            .Where(f => !string.IsNullOrWhiteSpace(f.Key))
            .GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var tokens = Tokenize(normalized);
        var index = 0;
        var root = ParseOr(tokens, ref index, lookup);

        if (index < tokens.Count && tokens[index].Type != TokenType.End)
            throw new RuleValidationException($"Unable to parse expression near '{tokens[index].Text}'.");

        return root;
    }

    public async Task<RuleFormConfigDto> LoadFormConfigAsync(long workflowId)
    {
        var workflow = await _workflowRepository.GetAsync(workflowId)
                       ?? throw new RuleValidationException("Workflow not found.");
        var config = workflow.RuleformConfig
                     ?? throw new RuleValidationException("This workflow has no form config assigned.");

        var ruleFields = await _ruleFieldRepository.GetListAsync();
        return BuildFormConfigDto(config, ruleFields.ToDictionary(x => x.Id));
    }

    public static RuleFormConfigDto BuildFormConfigDto(RuleFormConfig config, IReadOnlyDictionary<long, RuleField> fieldsById)
    {
        var fields = new List<RuleFormFieldDto>(config.RuleFormConfigFields.Count);
        foreach (var configField in config.RuleFormConfigFields)
        {
            if (!fieldsById.TryGetValue(configField.RulefieldId, out var field))
                throw new RuleValidationException("The workflow's form config references a field that no longer exists.");

            fields.Add(new RuleFormFieldDto
            {
                Id = configField.RulefieldId,
                Key = field.Key,
                DataType = field.DataType,
                AllowedOperators = ListExtensions.DeserializeIntList(configField.AllowedOperatorsJson),
                Label = field.Label,
                Options = ListExtensions.DeserializeStringList(field.OptionsJson),
                IsMultiple = configField.IsMultiple
            });
        }

        return new RuleFormConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            AllowNestedGroups = config.AllowNestedGroups,
            StrategyKey = config.StrategyKey,
            Fields = fields
        };
    }

    private static string StripOuterFinalization(string expression)
    {
        var trimmed = expression.Trim();

        if (trimmed.StartsWith("!(", StringComparison.Ordinal) && trimmed.EndsWith(")", StringComparison.Ordinal))
        {
            var inner = trimmed.Substring(2, trimmed.Length - 3).Trim();
            if (!string.IsNullOrWhiteSpace(inner))
                return inner;
        }

        return trimmed;
    }

    private RuleNodeDto ParseOr(List<Token> tokens, ref int index, IReadOnlyDictionary<string, RuleFormFieldDto> fields)
    {
        var left = ParseAnd(tokens, ref index, fields);

        while (Match(tokens, ref index, TokenType.Or))
        {
            var right = ParseAnd(tokens, ref index, fields);
            if (CanMergeSameFieldCondition(left, right))
            {
                left = MergeSameFieldCondition(left, right);
                continue;
            }

            left = Combine(left, right, LogicalOperator.Or);
        }

        return left;
    }

    private static bool CanMergeSameFieldCondition(RuleNodeDto left, RuleNodeDto right)
    {
        if (left.NodeType != RuleNodeType.Condition || right.NodeType != RuleNodeType.Condition)
            return false;

        if (string.IsNullOrWhiteSpace(left.FieldKey) || string.IsNullOrWhiteSpace(right.FieldKey))
            return false;

        if (!string.Equals(left.FieldKey, right.FieldKey, StringComparison.OrdinalIgnoreCase))
            return false;

        if (left.Operator != right.Operator)
            return false;

        return left.Operator is RuleOperator.Contains or RuleOperator.NotContains or RuleOperator.Equal or RuleOperator.NotEqual;
    }

    private static RuleNodeDto MergeSameFieldCondition(RuleNodeDto left, RuleNodeDto right)
    {
        var values = new List<string>();
        values.AddRange(ExtractConditionValues(left.Value));
        values.AddRange(ExtractConditionValues(right.Value));

        var unique = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RuleNodeDto
        {
            NodeType = RuleNodeType.Condition,
            FieldKey = left.FieldKey,
            Operator = left.Operator,
            Value = JsonSerializer.SerializeToElement(unique),
            IsMultiple = true,
            ActivityTypeId = left.ActivityTypeId ?? right.ActivityTypeId
        };
    }

    private static List<string> ExtractConditionValues(JsonElement? value)
    {
        if (!value.HasValue)
            return new List<string>();

        var element = value.Value;
        if (element.ValueKind == JsonValueKind.Array)
            return RuleTreeMapper.Strings(element);

        if (element.ValueKind == JsonValueKind.String)
            return string.IsNullOrWhiteSpace(element.GetString()) ? new List<string>() : new List<string> { element.GetString()! };

        return new List<string>();
    }

    private RuleNodeDto ParseAnd(List<Token> tokens, ref int index, IReadOnlyDictionary<string, RuleFormFieldDto> fields)
    {
        var left = ParsePrimary(tokens, ref index, fields);

        while (Match(tokens, ref index, TokenType.And))
        {
            var right = ParsePrimary(tokens, ref index, fields);
            left = Combine(left, right, LogicalOperator.And);
        }

        return left;
    }

    private RuleNodeDto ParsePrimary(List<Token> tokens, ref int index, IReadOnlyDictionary<string, RuleFormFieldDto> fields)
    {
        if (Match(tokens, ref index, TokenType.Not))
            return Negate(ParsePrimary(tokens, ref index, fields));

        if (Match(tokens, ref index, TokenType.LParen))
        {
            var inner = ParseOr(tokens, ref index, fields);
            Expect(tokens, ref index, TokenType.RParen, ")");
            return inner;
        }

        return ParseCondition(tokens, ref index, fields);
    }

    private RuleNodeDto ParseCondition(List<Token> tokens, ref int index, IReadOnlyDictionary<string, RuleFormFieldDto> fields)
    {
        if (tokens[index].Type == TokenType.Identifier && index + 1 < tokens.Count && tokens[index + 1].Type == TokenType.LParen)
            return ParseMethodCall(tokens, ref index, fields);

        var fieldToken = Expect(tokens, ref index, TokenType.Identifier, "field name");
        var fieldKey = fieldToken.Text;
        var field = RequireField(fieldKey, fields);

        var opToken = Expect(tokens, ref index, TokenType.Operator, "operator");
        var op = ParseRuleOperator(opToken.Text);
        var value = ParseLiteral(tokens, ref index);

        return CreateCondition(fieldKey, op, value, field);
    }

    private RuleNodeDto ParseMethodCall(List<Token> tokens, ref int index, IReadOnlyDictionary<string, RuleFormFieldDto> fields)
    {
        var methodNameToken = Expect(tokens, ref index, TokenType.Identifier, "method name");
        var methodName = methodNameToken.Text;
        Expect(tokens, ref index, TokenType.LParen, "(");

        var args = new List<object?>();
        if (tokens[index].Type != TokenType.RParen)
        {
            while (true)
            {
                args.Add(ParseMethodArgument(tokens, ref index));
                if (Match(tokens, ref index, TokenType.Comma))
                    continue;
                break;
            }
        }

        Expect(tokens, ref index, TokenType.RParen, ")");

        if (methodName == "LargeRuleLists.AnyCodes" || methodName == "LargeRuleLists.AllCodes")
        {
            if (args.Count != 2)
                throw new RuleValidationException($"Method '{methodName}' expects 2 arguments.");

            var fieldKey = args[0] as string ?? throw new RuleValidationException($"Method '{methodName}' requires a field name.");
            var field = RequireField(fieldKey, fields);
            var value = DecodeEncodedList(args[1]);
            return CreateCondition(fieldKey, RuleOperator.Contains, value, field);
        }

        if (methodName.EndsWith(".Contains", StringComparison.Ordinal))
        {
            var fieldKey = methodName.Substring(0, methodName.Length - ".Contains".Length);
            var field = RequireField(fieldKey, fields);
            var value = args.Count > 0 ? args[0] : throw new RuleValidationException($"Method '{methodName}' expects a value.");
            return CreateCondition(fieldKey, RuleOperator.Contains, value, field);
        }

        throw new RuleValidationException($"Unsupported method '{methodName}' in expression.");
    }

    private static object ParseMethodArgument(List<Token> tokens, ref int index)
    {
        if (tokens[index].Type == TokenType.String || tokens[index].Type == TokenType.Number || tokens[index].Type == TokenType.Identifier)
            return ParseLiteral(tokens, ref index);

        throw new RuleValidationException($"Unsupported method argument '{tokens[index].Text}'.");
    }

    private static object ParseLiteral(List<Token> tokens, ref int index)
    {
        var token = tokens[index];

        switch (token.Type)
        {
            case TokenType.String:
                index++;
                return token.Text;
            case TokenType.Number:
                index++;
                return token.Value ?? token.Text;
            case TokenType.Identifier:
                if (index + 1 < tokens.Count && tokens[index + 1].Type == TokenType.LParen)
                    throw new RuleValidationException($"Method calls must be parsed before literal parsing: '{token.Text}'.");

                index++;
                return token.Text;
            default:
                throw new RuleValidationException($"Unsupported literal '{token.Text}'.");
        }
    }

    private RuleNodeDto CreateCondition(string fieldKey, RuleOperator op, object value, RuleFormFieldDto field)
    {
        var payload = value is string[] arr ? arr : value;
        var element = JsonSerializer.SerializeToElement(payload);
        var isMultiple = field.IsMultiple &&( field.DataType == "List" || field.DataType == "Enum");

        var activityCodes = value switch
        {
            JsonElement json => RuleTreeMapper.Strings(json),
            string[] stringArray => stringArray.ToList(),
            IEnumerable<string> enumerable => enumerable.ToList(),
            string s => new List<string> { s },
            _ => new List<string>()
        };

        var typeId = activityCodes.Count > 0
            ? _activitySubTypeRepository.FirstOrDefaultAsync(x => activityCodes.Contains(x.iqvia_code)).GetAwaiter().GetResult()
            : null;

        return new RuleNodeDto
        {
            NodeType = RuleNodeType.Condition,
            FieldKey = fieldKey,
            Operator = op,
            Value = element,
            IsMultiple = isMultiple,
            ActivityTypeId = typeId?.ActivityType_id
        };
    }

    private static object DecodeEncodedList(object? value)
    {
        var text = value as string ?? throw new RuleValidationException("Encoded list value is missing.");
        var decoded = Convert.FromBase64String(text);
        return JsonSerializer.Deserialize<string[]>(decoded) ?? Array.Empty<string>();
    }

    private static RuleNodeDto Negate(RuleNodeDto node)
    {
        if (node.NodeType == RuleNodeType.Condition && node.Operator is not null)
        {
            node.Operator = FlipOperator(node.Operator.Value);
            return node;
        }

        return node;
    }

    private static RuleNodeDto Combine(RuleNodeDto left, RuleNodeDto right, LogicalOperator join)
    {
        var group = new RuleNodeDto
        {
            NodeType = RuleNodeType.Group,
            LogicalOperator = join
        };

        group.Children.Add(left);
        right.LogicalJoinOperator = join;
        group.Children.Add(right);
        return group;
    }

    private static RuleFormFieldDto RequireField(string key, IReadOnlyDictionary<string, RuleFormFieldDto> fields)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new RuleValidationException("Expression contains an empty field name.");

        if (!fields.TryGetValue(key, out var field))
            throw new RuleValidationException($"Field '{key}' was not found in the supplied field list.");

        return field;
    }

    private static RuleOperator ParseRuleOperator(string symbol) => symbol switch
    {
        "==" => RuleOperator.Equal,
        "!=" => RuleOperator.NotEqual,
        ">" => RuleOperator.GreaterThan,
        ">=" => RuleOperator.GreaterThanOrEqual,
        "<" => RuleOperator.LessThan,
        "<=" => RuleOperator.LessThanOrEqual,
        _ => throw new RuleValidationException($"Unsupported operator '{symbol}'.")
    };

    private static RuleOperator FlipOperator(RuleOperator op) => op switch
    {
        RuleOperator.Equal => RuleOperator.NotEqual,
        RuleOperator.NotEqual => RuleOperator.Equal,
        RuleOperator.Contains => RuleOperator.NotContains,
        RuleOperator.NotContains => RuleOperator.Contains,
        RuleOperator.GreaterThan => RuleOperator.LessThanOrEqual,
        RuleOperator.GreaterThanOrEqual => RuleOperator.LessThan,
        RuleOperator.LessThan => RuleOperator.GreaterThanOrEqual,
        RuleOperator.LessThanOrEqual => RuleOperator.GreaterThan,
        _ => op
    };

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var text = expression.Trim();
        var i = 0;

        while (i < text.Length)
        {
            var ch = text[i];

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '(')
            {
                tokens.Add(new Token(TokenType.LParen, ch.ToString()));
                i++;
                continue;
            }

            if (ch == ')')
            {
                tokens.Add(new Token(TokenType.RParen, ch.ToString()));
                i++;
                continue;
            }

            if (ch == ',')
            {
                tokens.Add(new Token(TokenType.Comma, ch.ToString()));
                i++;
                continue;
            }

            if (ch == '&' && i + 1 < text.Length && text[i + 1] == '&')
            {
                tokens.Add(new Token(TokenType.And, "&&"));
                i += 2;
                continue;
            }

            if (ch == '|' && i + 1 < text.Length && text[i + 1] == '|')
            {
                tokens.Add(new Token(TokenType.Or, "||"));
                i += 2;
                continue;
            }

            if (ch == '!')
            {
                if (i + 1 < text.Length && text[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, "!="));
                    i += 2;
                    continue;
                }

                tokens.Add(new Token(TokenType.Not, "!"));
                i++;
                continue;
            }

            if (ch == '=' && i + 1 < text.Length && text[i + 1] == '=')
            {
                tokens.Add(new Token(TokenType.Operator, "=="));
                i += 2;
                continue;
            }

            if (ch is '>' or '<')
            {
                if (i + 1 < text.Length && text[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, ch + "="));
                    i += 2;
                    continue;
                }

                tokens.Add(new Token(TokenType.Operator, ch.ToString()));
                i++;
                continue;
            }

            if (ch == '"')
            {
                var sb = new StringBuilder();
                i++;

                while (i < text.Length)
                {
                    var current = text[i];
                    if (current == '\\')
                    {
                        if (i + 1 >= text.Length)
                            throw new RuleValidationException("Invalid escape sequence in expression.");

                        var next = text[++i];
                        sb.Append(next switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '\\' => '\\',
                            '"' => '"',
                            _ => next
                        });
                        i++;
                        continue;
                    }

                    if (current == '"')
                    {
                        i++;
                        break;
                    }

                    sb.Append(current);
                    i++;
                }

                tokens.Add(new Token(TokenType.String, sb.ToString()));
                continue;
            }

            if (char.IsDigit(ch) || (ch == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
            {
                var start = i;
                i++;

                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
                    i++;

                var literal = text.Substring(start, i - start);
                tokens.Add(new Token(
                    TokenType.Number,
                    literal,
                    decimal.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : literal));
                continue;
            }

            if (char.IsLetter(ch) || ch == '_' || ch == '.')
            {
                var start = i;
                i++;

                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.'))
                    i++;

                var identifier = text.Substring(start, i - start);
                tokens.Add(new Token(TokenType.Identifier, identifier));
                continue;
            }

            throw new RuleValidationException($"Unsupported token '{ch}' in expression.");
        }

        tokens.Add(new Token(TokenType.End, string.Empty));
        return tokens;
    }

    private static bool Match(List<Token> tokens, ref int index, TokenType type)
    {
        if (index < tokens.Count && tokens[index].Type == type)
        {
            index++;
            return true;
        }

        return false;
    }

    private static Token Expect(List<Token> tokens, ref int index, TokenType type, string expectation, string? text = null)
    {
        if (index >= tokens.Count || tokens[index].Type != type)
            throw new RuleValidationException($"Expected {expectation} in expression.");

        var token = tokens[index];
        index++;

        if (!string.IsNullOrEmpty(text) && token.Text != text)
            throw new RuleValidationException($"Expected '{text}' but found '{token.Text}'.");

        return token;
    }

    private enum TokenType
    {
        End,
        LParen,
        RParen,
        Comma,
        And,
        Or,
        Not,
        Identifier,
        String,
        Number,
        Operator
    }

    private sealed record Token(TokenType Type, string Text, object? Value = null)
    {
        public override string ToString() => Text;
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

        var items = node.Children.Select((child, index) => new RenderedItem(
            Render(child, strategy, fields, isRoot: false),
            child.NodeType == RuleNodeType.Group,
            index == 0 ? LogicalOperator.And : child.LogicalJoinOperator ?? LogicalOperator.And)).ToList();

        return strategy.RenderGroup(node, items, isRoot);
    }
}
