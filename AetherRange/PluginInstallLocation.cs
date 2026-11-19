using System;
using System.IO;

namespace SocialDistance
{
    internal static class PluginInstallLocation
    {
        public static bool TryResolve(string registeredPath, string assemblyPath, out string target, out string error)
        {
            target = null;
            error = null;
            var candidate = string.IsNullOrWhiteSpace(registeredPath) ? assemblyPath : registeredPath;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                error = "ACT did not provide a registered plugin file path.";
                return false;
            }

            try
            {
                Uri uri;
                if (Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out uri) && uri.IsFile)
                    candidate = uri.LocalPath;
                candidate = Path.GetFullPath(candidate.Trim());
                if (!string.Equals(Path.GetFileName(candidate), "SocialDistance.dll", StringComparison.OrdinalIgnoreCase))
                {
                    error = "The registered plugin path is not SocialDistance.dll.";
                    return false;
                }
                if (!File.Exists(candidate))
                {
                    error = "The registered SocialDistance.dll was not found.";
                    return false;
                }
                target = candidate;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
                return false;
            }
        }
    }
}
