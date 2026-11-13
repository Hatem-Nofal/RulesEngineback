using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RulesEngine.WorkFlows.Rules.Helpers
{
    /// <summary>Kinds of codes in an import sheet that must exist in the database.</summary>
    internal enum CodeKind
    {
        Activity,
        Speciality,
        GenericGroup,
        Diagnosis,
        DiagnosisGroup
    }

    /// <summary>
    /// Collects every code found in the sheet so each kind is verified with a handful of set-based queries
    /// instead of one query per cell. Errors are reported back to the row (owner) that contained the code.
    /// </summary>
    internal sealed class PendingCodeChecks<TOwner> where TOwner : class
    {
        private readonly Dictionary<CodeKind, Dictionary<string, List<(TOwner Owner, string Spelling)>>> _byKind = new();

        /// <summary>Splits a cell on '|' and ',', trims, drops blanks and duplicates (case-insensitive).</summary>
        public static List<string> SplitCodes(string? values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return new List<string>();

            return values
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void Add(CodeKind kind, string? cellValue, TOwner owner)
        {
            foreach (var code in SplitCodes(cellValue))
            {
                if (!_byKind.TryGetValue(kind, out var codes))
                    _byKind[kind] = codes = new Dictionary<string, List<(TOwner, string)>>(StringComparer.OrdinalIgnoreCase);

                if (!codes.TryGetValue(code, out var owners))
                    codes[code] = owners = new List<(TOwner, string)>();

                owners.Add((owner, code));
            }
        }

        public IReadOnlyCollection<CodeKind> Kinds => _byKind.Keys;

        /// <summary>The distinct codes of one kind (case-insensitive), ready for a chunked IN query.</summary>
        public IReadOnlyCollection<string> CodesOf(CodeKind kind)
            => _byKind.TryGetValue(kind, out var codes) ? codes.Keys : Array.Empty<string>();

        public void ReportMissing(CodeKind kind, ISet<string> existing, Action<TOwner, string> report)
        {
            if (!_byKind.TryGetValue(kind, out var codes))
                return;

            foreach (var (code, owners) in codes)
            {
                if (existing.Contains(code))
                    continue;

                foreach (var (owner, spelling) in owners)
                    report(owner, spelling);
            }
        }
    }

    /// <summary>
    /// Hands out the numbers for generated rule names ("{workflow}-Rule-{n}") above every number already used in the
    /// workflow, so new rules never duplicate an existing name (the rules engine cannot compile a batch with duplicate names).
    /// </summary>
    internal sealed class RuleNameAllocator
    {
        private int _last;

        public RuleNameAllocator(string workflowName, IEnumerable<string?> existingNames)
        {
            var prefix = $"{workflowName}-Rule-";
            foreach (var name in existingNames)
            {
                if (name is not null
                    && name.StartsWith(prefix, StringComparison.Ordinal)
                    && int.TryParse(name.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
                    && n > _last)
                {
                    _last = n;
                }
            }
        }

        /// <summary>The next unused number (1-based).</summary>
        public int Next() => ++_last;
    }
}
