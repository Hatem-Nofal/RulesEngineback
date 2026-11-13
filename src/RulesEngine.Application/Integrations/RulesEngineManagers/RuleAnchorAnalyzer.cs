using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RulesEngine.Integrations.RulesEngineManagers;

/// <summary>
/// Finds the activity code(s) a stored rule expression is "anchored" on, so the validator can skip every rule whose
/// anchor does not match the activity being validated.
///
/// Soundness: the builder always emits <c>!( inner )</c>; a rule is violated only when <c>inner</c> is true. If the top-level
/// conjunction of <c>inner</c> contains <c>ctx.Activity == "X"</c> (or an OR of such equalities), then for any activity whose
/// code is not in that set <c>inner</c> is false, so the rule cannot be violated and need not be evaluated.
/// Anything that is not recognised with certainty returns null, which means "always evaluate this rule".
/// </summary>
internal static class RuleAnchorAnalyzer
{
    /// <summary>A rule anchored on more codes than this is cheaper to evaluate always than to duplicate into many buckets.</summary>
    public const int MaxAnchorCodes = 8;

    private static readonly Regex ActivityEquals = new(
        "^ctx\\.Activity\\s*==\\s*\"([^\"\\\\]*)\"$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <returns>The smallest set of activity codes the rule requires, or null when the rule is not safely anchorable.</returns>
    public static IReadOnlyList<string>? TryGetActivityAnchor(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var text = expression.Trim();
        if (text.Length < 4 || text[0] != '!' || text[1] != '(' || FindClosingParen(text, 1) != text.Length - 1)
            return null;   // not the "!( ... )" shape produced by the builder

        var terms = new List<string>();
        CollectConjuncts(text.Substring(2, text.Length - 3), terms);

        IReadOnlyList<string>? best = null;
        foreach (var term in terms)
        {
            var codes = TryReadCodes(term);
            if (codes is null || codes.Count == 0 || codes.Count > MaxAnchorCodes)
                continue;
            if (best is null || codes.Count < best.Count)
                best = codes;
        }

        return best;
    }

    // ---- conjunct extraction ----------------------------------------------------------------------

    /// <summary>Flattens nested AND groups: "(a && b) && c" yields a, b, c. OR terms are kept whole.</summary>
    private static void CollectConjuncts(string text, List<string> output)
    {
        text = text.Trim();
        if (text.Length == 0) return;

        if (IsWrappedInParens(text))
        {
            CollectConjuncts(text.Substring(1, text.Length - 2), output);
            return;
        }

        var orParts = SplitTopLevel(text, "||");
        if (orParts.Count > 1)
        {
            output.Add(text);   // an OR term: usable only if every branch is an activity equality
            return;
        }

        var andParts = SplitTopLevel(text, "&&");
        if (andParts.Count > 1)
        {
            foreach (var part in andParts)
                CollectConjuncts(part, output);
            return;
        }

        output.Add(text);
    }

    private static List<string>? TryReadCodes(string term)
    {
        term = term.Trim();
        while (IsWrappedInParens(term))
            term = term.Substring(1, term.Length - 2).Trim();

        var branches = SplitTopLevel(term, "||");
        var codes = new List<string>(branches.Count);
        foreach (var branch in branches)
        {
            var b = branch.Trim();
            while (IsWrappedInParens(b))
                b = b.Substring(1, b.Length - 2).Trim();

            var match = ActivityEquals.Match(b);
            if (!match.Success)
                return null;   // one branch is something else: the term proves nothing about the activity

            var code = match.Groups[1].Value;
            if (!codes.Contains(code, StringComparer.Ordinal))
                codes.Add(code);
        }

        return codes;
    }

    // ---- tiny scanner (string literals and parentheses aware) -------------------------------------

    private static bool IsWrappedInParens(string s)
        => s.Length >= 2 && s[0] == '(' && FindClosingParen(s, 0) == s.Length - 1;

    /// <summary>Index of the ')' matching the '(' at <paramref name="open"/>, or -1.</summary>
    private static int FindClosingParen(string s, int open)
    {
        var depth = 0;
        var inString = false;
        for (var i = open; i < s.Length; i++)
        {
            var c = s[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c == '(') depth++;
            else if (c == ')' && --depth == 0) return i;
        }
        return -1;
    }

    private static List<string> SplitTopLevel(string s, string op)
    {
        var parts = new List<string>();
        var depth = 0;
        var inString = false;
        var start = 0;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0 && i + 1 < s.Length && c == op[0] && s[i + 1] == op[1])
            {
                parts.Add(s.Substring(start, i - start));
                start = i + 2;
                i++;
            }
        }

        parts.Add(s.Substring(start));
        return parts;
    }
}
