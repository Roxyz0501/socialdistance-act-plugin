using System;

namespace SocialDistance
{
    internal static class LogMessageTriggerMatcher
    {
        public static bool IsMatch(string parsedLine, string originalLine, string configuredText)
        {
            var text = (configuredText ?? "").Trim();
            if (text.Length == 0)
                return false;

            return Contains(parsedLine, text) || Contains(originalLine, text);
        }

        private static bool Contains(string line, string text)
        {
            return !string.IsNullOrEmpty(line) &&
                   line.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
