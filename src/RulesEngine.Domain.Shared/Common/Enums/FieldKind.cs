using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Common.Enums
{
    public enum FieldKind
    {
        /// <summary>ctx.X is a collection of strings: Equal / NotEqual.</summary>
        Collection,
        /// <summary>ctx.X is a number: == != &gt; &gt;= &lt; &lt;=.</summary>
        Number,
        /// <summary>ctx.X is a string: Equal / NotEqual / In / NotIn.</summary>
        Text,
        /// <summary>ctx.X is a collection of strings: Contains / NotContains.</summary>
        MultipleSelect
    }
}
