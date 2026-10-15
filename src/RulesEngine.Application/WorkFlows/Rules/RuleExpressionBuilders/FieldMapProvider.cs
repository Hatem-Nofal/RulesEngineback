
using RulesEngine.Common.Consts;
using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;



/// <summary>
/// Builds a simple FieldMapDto for every RuleField in the catalog.
/// Path is derived from the field key by converting segments to PascalCase and prefixing with ctx.
/// Kind is inferred from the field DataType.
/// </summary>
public class FieldMapProvider: IFieldMapProvider
{
    private Dictionary<string, FieldMapDto>? _cache;

    private readonly IRepository<RuleField, Guid> _ruleFieldRepository;

    public FieldMapProvider(IRepository<RuleField, Guid> ruleFieldRepository)
    {
        _ruleFieldRepository = ruleFieldRepository;
    }

    public async Task<Dictionary<string, FieldMapDto>> GetAsync()
    {
        if (_cache is not null) return _cache;
        var fields = await _ruleFieldRepository.GetListAsync();
        var dict = new Dictionary<string, FieldMapDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in fields)
        {
            var path = "ctx." + string.Join('.', f.Key.Split('.').Select(s => ToPascal(s)));
            var kind = f.DataType switch
            {
                DataTypes.Number => FieldKind.Number,
                DataTypes.List => FieldKind.List,
                DataTypes.Enum => FieldKind.Text,
                DataTypes.DateTime => FieldKind.DateTime,
                DataTypes.Decimal => FieldKind.Decimal,
                 _ => FieldKind.Text
            };
            dict[f.Key] = new FieldMapDto(path, kind);
        }
        _cache = dict;
        return dict;
    }

    private static string ToPascal(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
    }
}
