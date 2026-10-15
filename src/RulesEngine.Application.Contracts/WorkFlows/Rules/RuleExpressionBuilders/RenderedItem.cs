using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.WorkFlows.Rules.RuleExpressionBuilders
{
    public record RenderedItem(string Text, bool IsGroup, LogicalOperator Join);

}
