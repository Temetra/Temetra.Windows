#nullable enable
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Temetra.Windows;

public static class FilteredWindowsEnumerator
{
    // EnumWindows continues until the last window is enumerated or the callback returns false
    public static void EnumWindows(Func<ProgramDetails, bool> callback)
    {
        BOOL lpEnumFunc(HWND hwnd, LPARAM lparam)
        {
            var item = GetWindowItem(hwnd);
            if (item != null) return callback(item);
            return true;
        }

        PInvoke.EnumWindows(lpEnumFunc, nint.Zero);
    }

    private static ProgramDetails? GetWindowItem(HWND hwnd)
    {
        // Exclude this application
        PInvokeHelpers.GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == Environment.ProcessId) return null;

        // Exclude windows without the Visible style
        if (!PInvoke.IsWindowVisible(hwnd)) return null;

        // Ignore windows that are cloaked
        if (PInvokeHelpers.IsWindowCloaked(hwnd)) return null;

        // Check windows without WS_EX_APPWINDOW
        var windowStylesEx = PInvokeHelpers.GetWindowStyleEx(hwnd);
        if (!windowStylesEx.HasFlag(WINDOW_EX_STYLE.WS_EX_APPWINDOW))
        {
            // Exclude tool windows, and windows that don't become foreground window when clicked
            var flags = WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
            if ((windowStylesEx & flags) != 0) return null;
        }

        // Create program details
        var filename = PInvokeHelpers.GetFilenameForProcess(processId);
        var item = ProgramDetails.GetFromFilename(filename);

        // If exe is ApplicationFrameHost, check children for details
        if (item.Executable == "ApplicationFrameHost.exe")
        {
            // Callback function for enumerating child windows
            BOOL lpEnumFunc(HWND hwnd, LPARAM lparam)
            {
                // Get process for child window
                PInvokeHelpers.GetWindowThreadProcessId(hwnd, out uint processId);
                var filename = PInvokeHelpers.GetFullProcessName(processId);

                // If process is not ApplicationFrameHost, set item and end enumeration
                if (Path.GetFileNameWithoutExtension(filename) != "ApplicationFrameHost")
                {
                    item = ProgramDetails.GetFromFilename(filename);
                    return false;
                }

                return true;
            }
            
            // Process child windows
            PInvoke.EnumChildWindows(hwnd, lpEnumFunc, nint.Zero);
        }

        return item;
    }
}
