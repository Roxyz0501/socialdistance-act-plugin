using System;
using System.Linq;

namespace SocialDistance
{
    internal static class EchoCommandParser
    {
        public static bool TryGetMessage(string line, out string message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (TryGetPipeMessage(line, out message))
                return true;

            return TryGetColonMessage(line, out message);
        }

        private static bool TryGetPipeMessage(string line, out string message)
        {
            message = null;
            var fields = line.Split('|');
            for (var index = 0; index < fields.Length; index++)
            {
                if (!string.Equals(fields[index].Trim(), "0038", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 00|timestamp|0038|speaker|message|hash
                if (index >= 2 && !fields[index - 2].Trim().EndsWith("00", StringComparison.Ordinal))
                    continue;
                if (index + 2 >= fields.Length)
                    continue;

                message = fields[index + 2].Trim();
                return true;
            }
            return false;
        }

        private static bool TryGetColonMessage(string line, out string message)
        {
            message = null;
            const string marker = "00:0038:";
            var markerIndex = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return false;

            var remainder = line.Substring(markerIndex + marker.Length);
            var speakerEnd = remainder.IndexOf(':');
            if (speakerEnd < 0)
                return false;

            message = remainder.Substring(speakerEnd + 1).Trim();
            var lastColon = message.LastIndexOf(':');
            if (lastColon >= 0)
            {
                var possibleHash = message.Substring(lastColon + 1);
                if ((possibleHash.Length == 8 || possibleHash.Length == 16) &&
                    possibleHash.All(Uri.IsHexDigit))
                    message = message.Substring(0, lastColon).Trim();
            }
            return true;
        }
    }
}
