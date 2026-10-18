using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace RulesEngine.Common.Helpers
{
    public static class LargeRuleLists
    {
        // Bound retained list data across rule edits. Cache entries are immutable.
        private const int MaxCachedCharacters = 8 * 1024 * 1024;
        private const int MaxCachedLists = 4096;
        private static readonly object CacheLock = new();
        private static readonly Dictionary<string, HashSet<string>> Cache = new(StringComparer.Ordinal);
        private static readonly Queue<string> CacheOrder = new();
        private static int _cachedCharacters;

        public static string Encode(IEnumerable<string?> codes) => Convert.ToBase64String(
            JsonSerializer.SerializeToUtf8Bytes(codes.Select(c => c ?? "").Distinct(StringComparer.Ordinal)));

        // First level: keyed by the string INSTANCE (reference equality). The encoded list is a constant inside the
        // compiled rule expression, so every evaluation passes the same instance. A hit is lock-free and does not hash
        // the (potentially huge) base64 text. Entries live exactly as long as the compiled rule keeps the string alive.
        private static readonly ConditionalWeakTable<string, HashSet<string>> ByReference = new();
        private static readonly ConditionalWeakTable<string, HashSet<string>>.CreateValueCallback DecodeShared = DecodeThroughContentCache;

        private static HashSet<string> Decode(string encoded)
            => ByReference.TryGetValue(encoded, out var hit) ? hit : ByReference.GetValue(encoded, DecodeShared);

        // Second level: keyed by content (bounded). Only reached once per string instance, e.g. after a rule reload
        // that produced an equal list again.
        private static HashSet<string> DecodeThroughContentCache(string encoded)
        {
            lock (CacheLock)
            {
                if (Cache.TryGetValue(encoded, out var cached)) return cached;
                var codes = new HashSet<string>(
                    JsonSerializer.Deserialize<string[]>(Convert.FromBase64String(encoded))!, StringComparer.Ordinal);
                if (encoded.Length > MaxCachedCharacters) return codes;
                while (CacheOrder.Count > 0 && (Cache.Count >= MaxCachedLists || _cachedCharacters + encoded.Length > MaxCachedCharacters))
                {
                    var oldest = CacheOrder.Dequeue();
                    Cache.Remove(oldest);
                    _cachedCharacters -= oldest.Length;
                }
                Cache.Add(encoded, codes);
                CacheOrder.Enqueue(encoded);
                _cachedCharacters += encoded.Length;
                return codes;
            }
        }

        public static bool ContainsCode(string encoded, string code) => Decode(encoded).Contains(code);

        public static bool AnyCodes(IEnumerable<string> current, string encoded)
        {
            var codes = Decode(encoded);
            foreach (var code in current)
            {
                if (codes.Contains(code)) return true;
            }
            return false;
        }

        public static bool AllCodes(IEnumerable<string> current, string encoded) => Decode(encoded).IsSubsetOf(current);

        public static bool CountsMatch(Dictionary<string, int> counts, string encoded, string op, decimal limit, bool all)
        {
            var codes = Decode(encoded);
            bool Matches(string code) => counts.TryGetValue(code, out var value) && Compare(value, op, limit);
            return all ? codes.All(Matches) : codes.Any(Matches);
        }

        public static bool QuantitiesMatch(IEnumerable<string> current, Dictionary<string, decimal>? quantities,
            string encoded, string op, decimal limit, bool all)
        {
            var present = new HashSet<string>(current, StringComparer.Ordinal);
            var codes = Decode(encoded);
            bool Matches(string code) => present.Contains(code) && quantities != null
                && quantities.TryGetValue(code, out var value) && Compare(value, op, limit);
            return all ? codes.All(Matches) : codes.Any(Matches);
        }

        public static bool ActivityQuantityMatch(IEnumerable<string> current, decimal quantity,
            string encoded, string op, decimal limit, bool all) =>
            all && Decode(encoded).Count == 0 ||
            (all ? AllCodes(current, encoded) : AnyCodes(current, encoded)) && Compare(quantity, op, limit);

        public static int SumCounts(Dictionary<string, int> counts, string encoded)
        {
            var total = 0;
            foreach (var code in Decode(encoded))
                if (counts.TryGetValue(code, out var count)) total = unchecked(total + count);
            return total;
        }

        private static bool Compare(decimal value, string op, decimal limit) => op switch
        {
            "==" or "=" => value == limit,
            "!=" or "<>" => value != limit,
            ">" => value > limit,
            ">=" => value >= limit,
            "<" => value < limit,
            "<=" => value <= limit,
            _ => throw new ArgumentException("Unsupported quantity operator", nameof(op))
        };
    }
}
