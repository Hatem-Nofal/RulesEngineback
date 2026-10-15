using RulesEngine.Common.Consts;
using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders
{
    public sealed record FieldMapDto(string key, FieldKind Kind)
    {
        /// <summary>Number -> Number; ItemList / CodeList -> Collection; String / Enum -> Text.</summary>
        public static FieldKind KindOf(string dataType) => dataType switch
        {
            DataTypes.Number => FieldKind.Number,
            DataTypes.List => FieldKind.List,
            DataTypes.Enum => FieldKind.List,
            DataTypes.DateTime => FieldKind.DateTime,
            DataTypes.Decimal => FieldKind.Decimal,
             _ => FieldKind.Text
        };
    }
}
