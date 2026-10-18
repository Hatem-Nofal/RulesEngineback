using System.Text.RegularExpressions;

namespace RulesEngine.IntegrationsNew.RulesEngineManagers;

internal static partial class WorkflowNameNormalizer
{
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    /// <summary>Collapses whitespace runs to one space, trims and upper-cases (the format returned as NormalizedWorkFlowName).</summary>
    public static string? Normalize(string? name)
        => string.IsNullOrEmpty(name) ? name : Whitespace().Replace(name, " ").Trim().ToUpperInvariant();
}
