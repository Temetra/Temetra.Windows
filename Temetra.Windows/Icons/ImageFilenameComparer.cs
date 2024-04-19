using System.Text.RegularExpressions;

namespace Temetra.Windows;

internal sealed partial class ImageFilenameComparer : IComparer<string>
{
    [GeneratedRegex("(?<Name>.*\\.(scale|targetsize))-(?<Size>[0-9]+)")]
    private static partial Regex FilenameRegex();

    public int Compare(string a, string b)
    {
        // Get parts of filename
        var re = FilenameRegex();
        var aParts = re.Match(Path.GetFileNameWithoutExtension(a));
        var bParts = re.Match(Path.GetFileNameWithoutExtension(b));

        // Compare name groups
        var aName = aParts.Groups["Name"].Value;
        var bName = bParts.Groups["Name"].Value;
        var result = aName.CompareTo(bName);

        // Compare size groups
        if (result == 0)
        {
            if (int.TryParse(aParts.Groups["Size"].Value, out int aNum) &&
                int.TryParse(bParts.Groups["Size"].Value, out int bNum))
            {
                if (aNum > bNum) return 1;
                else if (aNum < bNum) return -1;
            }
        }

        return 0;
    }
}
