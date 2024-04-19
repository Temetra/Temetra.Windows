using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.Storage.Packaging.Appx;
using System.Runtime.InteropServices;

namespace Temetra.Windows;

internal static partial class PInvokeHelpers
{
    // EnumPackageInfo continues until the last package is enumerated or the callback returns false
    public static unsafe void EnumPackageInfo(string fullName, Func<PACKAGE_INFO, bool> callback)
    {
        // Open package
        var result = PInvoke.OpenPackageInfoByFullName(fullName, out _PACKAGE_INFO_REFERENCE* packInfoRef);

        try
        {
            if (result == WIN32_ERROR.ERROR_SUCCESS)
            {
                // Get buffer size for package info
                uint bufferSize = 0;
                PInvoke.GetPackageInfo(packInfoRef, (uint)PackageConstants.PACKAGE_FILTER_ALL_LOADED, &bufferSize);

                if (bufferSize > 0)
                {
                    // Read package info into buffer
                    Span<byte> buffer = stackalloc byte[(int)bufferSize];
                    uint packageCount = 0;
                    fixed (byte* pBuffer = buffer)
                    {
                        result = PInvoke.GetPackageInfo(*packInfoRef, (uint)PackageConstants.PACKAGE_FILTER_ALL_LOADED, ref bufferSize, pBuffer, &packageCount);
                    }

                    // Read packages from buffer
                    if (packageCount > 0)
                    {
                        var packSize = Marshal.SizeOf(typeof(PACKAGE_INFO));
                        for (int idx = 0; idx < packageCount; idx++)
                        {
                            // Read one package
                            var pos = (idx * packSize);
                            var slice = buffer[pos..(pos + packSize)];
                            var packageInfo = MemoryMarshal.Read<PACKAGE_INFO>(slice);

                            // Pass to callback func
                            // Stop iterating if callback returns false
                            if (!callback(packageInfo)) break;
                        }
                    }
                }
            }

        }
        finally
        {
            // Close package ref
            PInvoke.ClosePackageInfo(packInfoRef);
        }
    }

    public static string GetPackagePath(string packageFullName)
    {
        string packagePath = string.Empty;

        if (!string.IsNullOrEmpty(packageFullName))
        {
            EnumPackageInfo(packageFullName, package =>
            {
                packagePath = package.path.ToString();
                return false;
            });
        }

        return packagePath;
    }

    public static PackageProperties GetPackageProperties(string packagePath)
    {
        PackageProperties result = null;

        var manifestPath = Path.Combine(packagePath, "AppXManifest.xml");

        if (Path.Exists(manifestPath))
        {
            PInvoke.SHCreateStreamOnFileEx(manifestPath, (uint)STGM.STGM_SHARE_DENY_NONE, 0, false, null, out IStream manifestStream);
            if (manifestStream != null)
            {
                result = new();

                var factory = (IAppxFactory)new AppxFactory();
                var reader = factory.CreateManifestReader(manifestStream);

                var id = reader.GetPackageId();
                var appName = id.GetName().ToString() ?? string.Empty;
                result.AppName = appName;

                var props = reader.GetProperties();
                result.DisplayName = props.TryGetStringValue("DisplayName", appName, packagePath);
                result.PublisherDisplayName = props.TryGetStringValue("PublisherDisplayName", appName, packagePath);
                result.Logo = props.TryGetStringValue("Logo", appName, packagePath);
                result.Description = props.TryGetStringValue("Description", appName, packagePath);

                var apps = reader.GetApplications();
                var app = apps.GetCurrent();
                result.Executable = app.TryGetStringValue("Executable", appName, packagePath);
                result.Logo = app.TryGetStringValue("Square44x44Logo", appName, packagePath, result.Logo);
            }
        }

        return result;
    }
}
