using System;

namespace NuGetTypeSearch
{
    public static class StringExtensions
    {
        public static string SubstringUntilLast(this string current, string value, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            if (string.IsNullOrEmpty(current)) return current;

            var index = current.LastIndexOf(value, comparisonType);
            if (index >= 0)
            {
                return current.Substring(0, index);
            }

            return current;
        }

        public static string SubstringAfterLast(this string current, string value, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            if (string.IsNullOrEmpty(current)) return current;

            var index = current.LastIndexOf(value, comparisonType);
            if (index >= 0 && index + 1 < current.Length)
            {
                return current.Substring(index + 1);
            }

            return current;
        }
    }
}