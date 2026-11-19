using System;
using System.IO;
using System.Security.Cryptography;

namespace SocialDistance.Updater
{
    public static class UpdateApplyEngine
    {
        public static string ReplaceAtomically(string staged, string target, Action<string> validateInstalledFile)
        {
            if (string.IsNullOrWhiteSpace(staged) || string.IsNullOrWhiteSpace(target) || validateInstalledFile == null)
                throw new ArgumentException("An update source, target, and validation callback are required.");
            staged = Path.GetFullPath(staged);
            target = Path.GetFullPath(target);
            if (!File.Exists(staged) || !File.Exists(target)) throw new FileNotFoundException("The update source or target is missing.");

            var directory = Path.GetDirectoryName(target);
            var replacement = Path.Combine(directory, ".SocialDistance-" + Guid.NewGuid().ToString("N") + ".new");
            var backup = Path.Combine(directory, "SocialDistance.dll." + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") +
                "-" + Guid.NewGuid().ToString("N") + ".bak");
            var replaced = false;
            try
            {
                var stagedHash = Hash(staged);
                File.Copy(staged, replacement, false);
                if (!FixedTimeEquals(stagedHash, Hash(replacement))) throw new IOException("Staged DLL copy did not verify.");
                File.Replace(replacement, target, backup, false);
                replaced = true;
                validateInstalledFile(target);
                if (!FixedTimeEquals(stagedHash, Hash(target))) throw new IOException("Installed DLL verification failed.");
                return backup;
            }
            catch (Exception ex)
            {
                if (replaced && File.Exists(backup))
                {
                    try { File.Copy(backup, target, true); }
                    catch (Exception rollback) { throw new AggregateException("Update failed and rollback also failed.", ex, rollback); }
                }
                throw;
            }
            finally
            {
                try { if (File.Exists(replacement)) File.Delete(replacement); }
                catch { }
            }
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= char.ToUpperInvariant(left[i]) ^ char.ToUpperInvariant(right[i]);
            return difference == 0;
        }
    }
}
