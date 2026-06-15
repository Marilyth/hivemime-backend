using System.Text;

public static class StringHelper
{
    /// <summary>
    /// Returns a normalized version of the string, where all letters are lowercase, multiple whitespace characters are replaced with a single space,
    /// and all non-alphanumeric characters are removed (except for spaces).
    /// </summary>
    /// <param name="s">The string to normalize.</param>
    /// <param name="allowWhitespace">Whether to allow whitespace in the normalized string. If false, all whitespace will be removed.</param>
    /// <returns>The normalized string.</returns>
    public static string Normalize(this string s, bool allowWhitespace)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;

        var sb = new StringBuilder(s.Length);
        bool isLastSpace = false;
        
        foreach (var c in s.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                isLastSpace = false;
            }

            else if (char.IsWhiteSpace(c) && allowWhitespace && !isLastSpace)
            {
                sb.Append(' ');
                isLastSpace = true;
            }
        }

        return sb.ToString().Trim();
    }
}