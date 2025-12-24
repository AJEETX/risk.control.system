using System.Text.RegularExpressions;

namespace risk.control.system.Helpers
{
    public static class RegexHelper
    {
        private const string validDomainExpression = "^[a-zA-Z0-9.-]+$";
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(200);

        public static bool IsMatch(string input)
        {
            return Regex.IsMatch(
                input,
                validDomainExpression,
                RegexOptions.IgnoreCase,
                DefaultTimeout);
        }
    }
}
