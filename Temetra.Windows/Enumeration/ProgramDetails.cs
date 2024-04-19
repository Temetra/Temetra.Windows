using System.Diagnostics;

namespace Temetra.Windows;

public class ProgramDetails
{
    public string Path { get; set; }
    public string Executable { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }

    public override string ToString()
    {
        return $"Desc: {Description}\n\t Path: {Path}\n\t Exe: {Executable}\n\t Image: {Image}";
    }

    public static ProgramDetails GetFromFilename(string filename)
    {
        if (File.Exists(filename))
        {
            // Create details
            ProgramDetails item = new()
            {
                Path = System.IO.Path.GetDirectoryName(filename) ?? string.Empty
            };

            // Look for AppXPackage properties
            var props = PInvokeHelpers.GetPackageProperties(item.Path);

            if (props != null)
            {
                // Set using props
                item.Executable = props.Executable;
                item.Description = props.DisplayName;
                item.Image = props.Logo;
            }
            else
            {
                // Otherwise get data from file
                var version = FileVersionInfo.GetVersionInfo(filename);
                item.Executable = System.IO.Path.GetFileName(filename) ?? string.Empty;
                item.Description = version.FileDescription;
            }

            // Give description a value if missing
            if (string.IsNullOrEmpty(item.Description))
            {
                item.Description = System.IO.Path.GetFileNameWithoutExtension(item.Executable);
            }

            return item;
        }
        else
        {
            return null;
        }
    }
}
