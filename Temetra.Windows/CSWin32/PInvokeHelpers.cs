using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Dwm;

namespace Temetra.Windows;

internal static partial class PInvokeHelpers
{
    public static unsafe uint GetWindowThreadProcessId(HWND hwnd, out uint processId)
    {
        fixed (uint* pId = &processId)
        {
            return PInvoke.GetWindowThreadProcessId(hwnd, pId);
        }
    }

    public static unsafe string GetWindowText(HWND hwnd)
    {
        const int MAX_BUFFER_LEN = 4096;

        Span<char> buffer = stackalloc char[MAX_BUFFER_LEN];
        int len;

        fixed (char* pBuffer = buffer)
        {
            len = PInvoke.GetWindowText(hwnd, pBuffer, MAX_BUFFER_LEN);
        }

        return buffer[..len].ToString();
    }

    public static unsafe string GetClassName(HWND hwnd)
    {
        const int MAX_BUFFER_LEN = 256;

        Span<char> buffer = stackalloc char[MAX_BUFFER_LEN];
        int len;

        fixed (char* pBuffer = buffer)
        {
            len = PInvoke.GetClassName(hwnd, pBuffer, MAX_BUFFER_LEN);
        }

        return buffer[..len].ToString();
    }

    public static unsafe bool IsWindowCloaked(HWND hwnd)
    {
        int isCloaked;
        var result = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &isCloaked, sizeof(int));
        return (result == 0 && isCloaked != 0);
    }

    public static WINDOW_EX_STYLE GetWindowStyleEx(HWND hwnd)
    {
        return (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
    }

    public static unsafe string LoadIndirectString(string source)
    {
        const int MAX_BUFFER_LEN = 32767;

        Span<char> buffer = stackalloc char[MAX_BUFFER_LEN];
        uint len = MAX_BUFFER_LEN;

        fixed (char* pBuffer = buffer)
        {
            var result = PInvoke.SHLoadIndirectString(source, pBuffer, len);
            if (!result.Succeeded) return string.Empty;
        }

        var idx = buffer.IndexOf('\0');
        var range = idx < 0 ? ..(int)len : ..idx;
        return buffer[range].ToString();
    }

    public static string LoadIndirectString(string value, string appName, string packagePath)
    {
        if (!string.IsNullOrEmpty(appName) && value.StartsWith("ms-resource:"))
        {
            var resource = value[12..];
            var source = $"@{{{packagePath}\\resources.pri?ms-resource://{appName}/Resources/{resource}}}";
            return LoadIndirectString(source);
        }
        return value;
    }

    public static string GetFilenameForProcess(uint processId)
    {
        try
        {
            // This is better for WinStore apps, but doesn't work for programs that are elevated
            var process = System.Diagnostics.Process.GetProcessById((int)processId);
            return process.MainModule.FileName;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // This backup method words
            return GetFullProcessName(processId);
        }
    }
}
