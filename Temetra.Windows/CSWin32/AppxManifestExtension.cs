#nullable enable
using Windows.Win32;
using Windows.Win32.Storage.Packaging.Appx;

namespace Temetra.Windows;

internal static class AppxManifestExtension
{
    public static string TryGetStringValue(this IAppxManifestProperties props, string key, string defaultValue = "")
    {
        try
        {
            return props.GetStringValue(key).ToString() ?? string.Empty;
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    public static string TryGetStringValue(this IAppxManifestProperties props, string key, string appName, string packagePath, string defaultValue = "")
    {
        var value = props.TryGetStringValue(key, defaultValue);
        return PInvokeHelpers.LoadIndirectString(value, appName, packagePath);
    }

    public static string TryGetStringValue(this IAppxManifestApplication app, string key, string defaultValue = "")
    {
        try
        {
            return app.GetStringValue(key).ToString() ?? string.Empty;
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    public static string TryGetStringValue(this IAppxManifestApplication app, string key, string appName, string packagePath, string defaultValue = "")
    {
        var value = app.TryGetStringValue(key, defaultValue);
        return PInvokeHelpers.LoadIndirectString(value, appName, packagePath);
    }
}
