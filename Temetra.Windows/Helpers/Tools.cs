using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Temetra.Windows;

public static class Tools
{
    public static void BringWindowToFront(nint windowHandle)
    {
        PInvoke.SetForegroundWindow((HWND)windowHandle);
    }

    [SupportedOSPlatform("windows10.0.14393")]
    public static void CenterAndResizeWindow(nint windowHandle, uint width, uint height)
    {
        HWND hwnd = (HWND)windowHandle;

        // Get monitor info
        HMONITOR monitor = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        MONITORINFO lpmi = new() { cbSize = 40 };
        PInvoke.GetMonitorInfo(monitor, ref lpmi);
        var monitorX = (lpmi.rcMonitor.left + lpmi.rcMonitor.right) / 2d;
        var monitorY = (lpmi.rcMonitor.bottom + lpmi.rcMonitor.top) / 2d;

        // Scale width and height
        var dpi = PInvoke.GetDpiForWindow(hwnd);
        var scale = dpi / 96d;
        var scaledWidth = width * scale;
        var scaledHeight = height * scale;

        // Get x and y
        var x = monitorX - (scaledWidth / 2d);
        var y = monitorY - (scaledHeight / 2d);

        // Set window size and pos
        PInvoke.SetWindowPos(hwnd, (HWND)0, (int)x, (int)y, (int)scaledWidth, (int)scaledHeight, 0);
    }
}
