using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RulesEngine.Common.Extensions
{
    public static class ListExtensions
    {
        public static List<int>? DeserializeIntList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // Handle double-serialized JSON
            if (json.TrimStart().StartsWith('"'))
            {
                json = JsonSerializer.Deserialize<string>(json);
            }

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<List<int>>(json);
        }
        public static List<string>? DeserializeStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // Handle double-serialized JSON
            if (json.TrimStart().StartsWith('"'))
            {
                json = JsonSerializer.Deserialize<string>(json);
            }

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<List<string>>(json);
        }
    }
}
