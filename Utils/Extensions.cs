using System.Text.RegularExpressions;

namespace Pipeline
{
    public static class Extensions
    {
        public static string RemoveExtraSpaces(this string input) =>
        Regex.Replace(input, @"\s+", " ");
    }
}