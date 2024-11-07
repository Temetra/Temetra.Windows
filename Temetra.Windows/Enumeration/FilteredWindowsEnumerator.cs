#nullable enable
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Temetra.Windows;

public enum EnumerationResultStatus
{
    Successful,
    Excluded,
    Invisible,
    Cloaked,
    ToolWindow,
    NoDetails
}

public class EnumerationResult
{
    // Status of the query
    public EnumerationResultStatus Status { get; set; }

    // Context for unsuccessful query - HWND or file path
    public string? Context { get; set; }

    // Programe details if query successful
    public ProgramDetails? Details { get; set; }
}

public static class FilteredWindowsEnumerator
{
    // EnumWindows continues until the last window is enumerated or the callback returns false
    public static void EnumWindows(Func<EnumerationResult, bool> callback)
    {
        BOOL lpEnumFunc(HWND hwnd, LPARAM lparam)
        {
            var item = GetWindowItem(hwnd);
            if (item != null) return callback(item);
            return true;
        }

        PInvoke.EnumWindows(lpEnumFunc, nint.Zero);
    }

    private static EnumerationResult GetWindowItem(HWND hwnd)
    {

        // Exclude this application
        PInvokeHelpers.GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == Environment.ProcessId)
        {
            return new EnumerationResult
            {
                Status = EnumerationResultStatus.Excluded,
                Context = hwnd.ToString()
            };
        }

        // Exclude windows without the Visible style
        if (!PInvoke.IsWindowVisible(hwnd))
        {
            return new EnumerationResult
            {
                Status = EnumerationResultStatus.Invisible,
                Context = hwnd.ToString()
            };
        }

        // Ignore windows that are cloaked
        if (PInvokeHelpers.IsWindowCloaked(hwnd))
        {
            return new EnumerationResult
            {
                Status = EnumerationResultStatus.Cloaked,
                Context = hwnd.ToString()
            };
        }

        // Check windows without WS_EX_APPWINDOW
        var windowStylesEx = PInvokeHelpers.GetWindowStyleEx(hwnd);
        if (!windowStylesEx.HasFlag(WINDOW_EX_STYLE.WS_EX_APPWINDOW))
        {
            // Exclude tool windows, and windows that don't become foreground window when clicked
            var flags = WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
            if ((windowStylesEx & flags) != 0)
            {
                return new EnumerationResult
                {
                    Status = EnumerationResultStatus.ToolWindow,
                    Context = hwnd.ToString()
                };
            }
        }

        // Create program details
        var filename = PInvokeHelpers.GetFilenameForProcess(processId);
        var item = ProgramDetails.GetFromFilename(filename);

        // Failed to get details, probably permissions issue
        if (item == null) return new EnumerationResult
        {
            Status = EnumerationResultStatus.NoDetails,
            Context = filename
        };

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

        return new EnumerationResult { Status = EnumerationResultStatus.Successful, Details = item };
    }
}
