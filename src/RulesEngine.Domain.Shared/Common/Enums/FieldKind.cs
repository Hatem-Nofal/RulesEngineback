namespace RulesEngine.Common.Enums
{
    public enum FieldKind
    {
        /// <summary>ctx.X is a collection of strings: Equal / NotEqual.</summary>
        List = 1,
        /// <summary>ctx.X is a number: == != &gt; &gt;= &lt; &lt;=.</summary>
        Number = 2,
        /// <summary>ctx.X is a string: Equal / NotEqual / In / NotIn.</summary>
        Text = 3,
        /// <summary>ctx.X is a decimal: == != &gt; &gt;= &lt; &lt;=.</summary>
        Decimal = 4,
        /// <summary>ctx.X is a datetime: == != &gt; &gt;= &lt; &lt;=.</summary>
        DateTime = 5
    }
}
