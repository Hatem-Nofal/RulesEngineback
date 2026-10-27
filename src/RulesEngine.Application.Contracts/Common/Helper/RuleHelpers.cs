using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RulesEngine.Common.Helper
{
    public static class RuleHelpers
    {
      
        public static string FormatMessage(
     string template,
     IDictionary<string, object> parameters)
        {
            return Regex.Replace(template, @"\{\s*(\w+)\s*\}", match =>
            {
                var propertyName = match.Groups[1].Value;

                // Get the object stored under "ctx"
                if (!parameters.TryGetValue("ctx", out var context) ||
                    context is null)
                {
                    return match.Value;
                }

                var property = context.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

                if (property is null)
                    return match.Value;

                var value = property.GetValue(context);

                return value switch
                {
                    null => string.Empty,
                    IEnumerable<string> items => string.Join(", ", items),
                    _ => value.ToString() ?? string.Empty
                };
            });
        }
        public static object? GetPropertyValue(
    string propertyName,
    IDictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue("ctx", out var context) ||
                context is null)
            {
                return null;
            }

            var property = context.GetType().GetProperty(
                propertyName,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);

            return property?.GetValue(context);
        }
    }
}
