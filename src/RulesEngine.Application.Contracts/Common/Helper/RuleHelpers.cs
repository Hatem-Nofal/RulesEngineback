using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System.Linq;

namespace RulesEngine.Common.Helper
{
    public static class RuleHelpers
    {
        private static readonly Regex PlaceholderRegex = new(@"\{\s*(\w+)\s*\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Property lookup by (context type, name) is reflection: do it once per pair, not once per failure.
        private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> PropertyCache = new();

        private static PropertyInfo? FindProperty(Type type, string name)
            => PropertyCache.GetOrAdd((type, name), static key => key.Type.GetProperty(
                key.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));


        public static string FormatMessage(
     string template,
     IDictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            return PlaceholderRegex.Replace(template, match =>
            {
                var propertyName = match.Groups[1].Value;

                // Get the object stored under "ctx"
                if (!parameters.TryGetValue("ctx", out var context) ||
                    context is null)
                {
                    return match.Value;
                }

                var property = FindProperty(context.GetType(), propertyName);

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

            return FindProperty(context.GetType(), propertyName)?.GetValue(context);
        }


        public static string ReplacePlaceholders(
    this string template,
    RuleNodeDto root)
        {
            var values = root.GetAllNodes()
                .Where(x => !string.IsNullOrWhiteSpace(x.FieldKey))
                .GroupBy(x => x.FieldKey!)
                .ToDictionary(
                    g => g.Key,
                    g => new Queue<string>(
                        g.Select(x => x.Value?.ToString() ?? string.Empty)
                    )
                );

            return Regex.Replace(template, @"\{([^{}]+)\}", match =>
            {
                var key = match.Groups[1].Value;

                if (!values.TryGetValue(key, out var queue) ||
                    queue.Count == 0)
                {
                    return match.Value;
                }

                return queue.Dequeue();
            });
        }

        public static IEnumerable<RuleNodeDto> GetAllNodes(this RuleNodeDto node)
        {
            yield return node;

            foreach (var child in node.Children ?? [])
            {
                foreach (var descendant in child.GetAllNodes())
                {
                    yield return descendant;
                }
            }
        }
    }
}
