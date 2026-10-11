using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RulesEngine.Common.Extensions
{
    public static class StringExtensions
    {
        public static string NormalizeSpace(this string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return string.Concat(
                    value.Normalize(NormalizationForm.FormKC)
                         .Where(c =>
                             !char.IsWhiteSpace(c) &&
                             !char.IsControl(c) &&
                             c != '\u200B' && // Zero-width space
                             c != '\u200C' && // Zero-width non-joiner
                             c != '\u200D' && // Zero-width joiner
                             c != '\uFEFF'    // BOM / zero-width no-break space
                         ))
                .ToUpperInvariant();
        }

    }
}
