using System;
using System.Linq;

namespace SocialDistance
{
    internal sealed class SemVersion : IComparable<SemVersion>
    {
        private SemVersion(int major, int minor, int patch, string prerelease)
        {
            Major = major; Minor = minor; Patch = patch; Prerelease = prerelease ?? "";
        }

        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Patch { get; private set; }
        public string Prerelease { get; private set; }
        public bool IsStable => Prerelease.Length == 0;

        public static bool TryParse(string value, out SemVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase)) text = text.Substring(1);
            var plus = text.IndexOf('+');
            if (plus >= 0) text = text.Substring(0, plus);
            var dash = text.IndexOf('-');
            var prerelease = dash >= 0 ? text.Substring(dash + 1) : "";
            var core = dash >= 0 ? text.Substring(0, dash) : text;
            var parts = core.Split('.');
            int major, minor, patch = 0;
            if (parts.Length < 2 || parts.Length > 3 ||
                !int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor) ||
                (parts.Length == 3 && !int.TryParse(parts[2], out patch)))
                return false;
            if (parts.Length == 2) patch = 0;
            if (major < 0 || minor < 0 || patch < 0) return false;
            if (dash >= 0 && (prerelease.Length == 0 || prerelease.Split('.').Any(x => x.Length == 0)))
                return false;
            version = new SemVersion(major, minor, patch, prerelease);
            return true;
        }

        public int CompareTo(SemVersion other)
        {
            if (other == null) return 1;
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;
            if (IsStable && !other.IsStable) return 1;
            if (!IsStable && other.IsStable) return -1;
            if (IsStable) return 0;
            var left = Prerelease.Split('.');
            var right = other.Prerelease.Split('.');
            for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
            {
                if (i >= left.Length) return -1;
                if (i >= right.Length) return 1;
                int leftNumber, rightNumber;
                var leftNumeric = int.TryParse(left[i], out leftNumber);
                var rightNumeric = int.TryParse(right[i], out rightNumber);
                if (leftNumeric && rightNumeric) result = leftNumber.CompareTo(rightNumber);
                else if (leftNumeric != rightNumeric) result = leftNumeric ? -1 : 1;
                else result = string.Compare(left[i], right[i], StringComparison.Ordinal);
                if (result != 0) return result;
            }
            return 0;
        }

        public override string ToString() => Major + "." + Minor + "." + Patch +
                                             (IsStable ? "" : "-" + Prerelease);
    }
}
