using System.Text.RegularExpressions;

namespace Sciuridae.Api.Data;

public class VersionComparer : IComparer<Release>
{
    public static VersionComparer Instance { get; } = new();

    public int Compare(Release? x, Release? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        return CompareVersions(x.Version, y.Version);
    }

    private static int CompareVersions(string xVersion, string yVersion)
    {
        Regex versionRegex = new(@"^(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?<Suffix>.*)$");
        Match xMatch = versionRegex.Match(xVersion);
        Match yMatch = versionRegex.Match(yVersion);
        if (!xMatch.Success || !yMatch.Success)
        {
            return string.Compare(xVersion, yVersion, StringComparison.Ordinal);
        }
        //NB: numeric comparson for Major, Minor, Patch, string comparison for Suffix
        int rv = CompareGroup("Major");
        if (rv != 0)
        {
            return rv;
        }

        rv = CompareGroup("Minor");
        if (rv != 0)
        {
            return rv;
        }

        rv = CompareGroup("Patch");
        if (rv != 0)
        {
            return rv;
        }
        
        return xMatch.Groups["Suffix"].Value.CompareTo(yMatch.Groups["Suffix"].Value);

        int CompareGroup(string groupName)
        {
            int xValue = int.Parse(xMatch.Groups[groupName].Value);
            int yValue = int.Parse(yMatch.Groups[groupName].Value);
            return xValue.CompareTo(yValue);
        }

    }
}
