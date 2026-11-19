using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SocialDistance.Updater
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length != 7 || args[0] != "--apply") return 2;
            var target = Path.GetFullPath(args[1]);
            var staged = Path.GetFullPath(args[2]);
            int processId;
            long startTicks;
            if (!int.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out processId) ||
                !long.TryParse(args[4], NumberStyles.None, CultureInfo.InvariantCulture, out startTicks)) return 2;
            var expectedVersion = args[5];
            var language = args[6];

            if (!string.Equals(Path.GetFileName(target), "SocialDistance.dll", StringComparison.OrdinalIgnoreCase) ||
                !IsInUpdateStaging(staged) || !File.Exists(target) || !File.Exists(staged))
                return 3;
            var expectedUpdater = Path.Combine(Path.GetDirectoryName(staged), "SocialDistance.Updater.exe");
            if (!string.Equals(Path.GetFullPath(Assembly.GetExecutingAssembly().Location), expectedUpdater,
                    StringComparison.OrdinalIgnoreCase)) return 3;

            try
            {
                ValidateUpdater(expectedVersion);
                if (!WaitForActExit(processId, startTicks))
                    throw new InvalidOperationException("Could not safely wait for ACT to exit.");
                ValidatePlugin(staged, expectedVersion);
                UpdateApplyEngine.ReplaceAtomically(staged, target, path => ValidatePlugin(path, expectedVersion));
                ShowMessage(language, "success");
                return 0;
            }
            catch (Exception ex)
            {
                ShowMessage(language, "failure", ex.GetBaseException().Message);
                return 1;
            }
        }

        private static bool IsInUpdateStaging(string path)
        {
            var updatesRoot = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SocialDistance", "Updates")) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(path);
            return full.StartsWith(updatesRoot, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Path.GetFileName(full), "SocialDistance.dll", StringComparison.OrdinalIgnoreCase);
        }

        private static bool WaitForActExit(int processId, long startTicks)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (process.StartTime.ToUniversalTime().Ticks != startTicks) return true;
                    process.WaitForExit();
                }
                return true;
            }
            catch (ArgumentException) { return true; }
            catch (InvalidOperationException) { return true; }
            catch (Win32Exception) { return false; }
        }

        private static void ValidatePlugin(string path, string expectedVersion)
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            if (!string.Equals(assemblyName.Name, "SocialDistance", StringComparison.Ordinal))
                throw new InvalidDataException("Unexpected plugin assembly.");
            var version = assemblyName.Version.Major + "." + assemblyName.Version.Minor + "." + Math.Max(0, assemblyName.Version.Build);
            if (!string.Equals(version, expectedVersion, StringComparison.Ordinal))
                throw new InvalidDataException("Plugin version does not match the release.");
            if (!string.Equals(FileVersionInfo.GetVersionInfo(path).CompanyName, "Roxyz0501", StringComparison.Ordinal))
                throw new InvalidDataException("Plugin author metadata is invalid.");
        }

        private static void ValidateUpdater(string expectedVersion)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            var name = AssemblyName.GetAssemblyName(path);
            if (!string.Equals(name.Name, "SocialDistance.Updater", StringComparison.Ordinal))
                throw new InvalidDataException("Unexpected update helper assembly.");
            var version = name.Version.Major + "." + name.Version.Minor + "." + Math.Max(0, name.Version.Build);
            if (!string.Equals(version, expectedVersion, StringComparison.Ordinal))
                throw new InvalidDataException("Update helper version does not match the release.");
            if (!string.Equals(FileVersionInfo.GetVersionInfo(path).CompanyName, "Roxyz0501", StringComparison.Ordinal))
                throw new InvalidDataException("Update helper author metadata is invalid.");
        }

        private static void ShowMessage(string language, string kind, string detail = null)
        {
            var ja = language.StartsWith("ja", StringComparison.OrdinalIgnoreCase);
            var zh = language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            var ko = language.StartsWith("ko", StringComparison.OrdinalIgnoreCase);
            var title = "SocialDistance";
            string message;
            if (kind == "success")
                message = ja ? "更新を適用しました。ACTを再起動してください。" : zh ? "更新已完成。请重新启动 ACT。" : ko ? "업데이트가 완료되었습니다. ACT를 다시 시작하세요." : "The update was installed. Please restart ACT.";
            else
                message = (ja ? "更新に失敗しました。バックアップから復元を試みました。\r\n" : zh ? "更新失败。已尝试从备份恢复。\r\n" : ko ? "업데이트에 실패했습니다. 백업에서 복구를 시도했습니다.\r\n" : "Update failed. A rollback from backup was attempted.\r\n") + detail;
            MessageBox.Show(message, title, MessageBoxButtons.OK,
                kind == "success" ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

    }
}
