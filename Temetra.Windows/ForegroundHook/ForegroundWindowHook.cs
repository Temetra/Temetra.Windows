using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Temetra.Windows;

public class ForegroundWindowHook : IDisposable
{
    private readonly WINEVENTPROC hookDelegate;
    private HWINEVENTHOOK hookInstance;
    private HWND lastHandle;
    private uint lastDwmsEventTime = 0;
    private Timer updateTimer;
    private const int timerDelay = 1000;
    private bool disposed = false;

    private static readonly string explorerPath = Path.Combine(
        Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System))?.FullName ?? "",
        "explorer.exe"
    );

    public event EventHandler<ForegroundWindowChangedEventArgs> ForegroundWindowChanged;

    public ForegroundWindowHook()
    {
        hookDelegate = new WINEVENTPROC(Callback);
        hookInstance = HWINEVENTHOOK.Null;
        lastHandle = HWND.Null;
    }

    ~ForegroundWindowHook()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing) { }
        StopHook();
        disposed = true;
    }

    public void StartHook()
    {
        if (hookInstance.IsNull)
        {
            hookInstance = PInvoke.SetWinEventHook(
                eventMin: (uint)WindowsEventHookType.EVENT_SYSTEM_FOREGROUND,
                eventMax: (uint)WindowsEventHookType.EVENT_SYSTEM_MINIMIZEEND,
                hmodWinEventProc: HMODULE.Null,
                pfnWinEventProc: hookDelegate,
                idProcess: 0,
                idThread: 0,
                dwFlags: 0);

            System.Diagnostics.Debug.WriteLine($"Foreground Hook = {hookInstance}");

            updateTimer = new Timer(TimerCallback, null, timerDelay, timerDelay);
        }
    }

    public void StopHook()
    {
        if (!hookInstance.IsNull)
        {
            updateTimer.Dispose();

            if (!PInvoke.UnhookWinEvent(hookInstance))
            {
                System.Diagnostics.Debug.WriteLine($"Failed to unhook foreground events");
            }

            hookInstance = HWINEVENTHOOK.Null;
        }
    }

    private void TimerCallback(object state)
    {
        var hwnd = PInvoke.GetForegroundWindow();
        ProcessWindow(hwnd, fromTimer: true, elapsed: 0);
    }

    private void Callback(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // Restart timer
        updateTimer.Change(timerDelay, timerDelay);

        // Only process events related to FG capture
        var eventTypeEnum = (WindowsEventHookType)eventType;
        if (eventTypeEnum != WindowsEventHookType.EVENT_SYSTEM_FOREGROUND && eventTypeEnum != WindowsEventHookType.EVENT_SYSTEM_MINIMIZEEND)
        {
            return;
        }

        // Only process events with valid parameters
        if (hwnd.IsNull || idObject != 0)
        {
            return;
        }

        // Calc elapsed time
        var elapsed = dwmsEventTime - lastDwmsEventTime;
        lastDwmsEventTime = dwmsEventTime;

        // Process data
        ProcessWindow(hwnd, fromTimer: false, elapsed: elapsed);
    }

    private void ProcessWindow(HWND hwnd, bool fromTimer, uint elapsed)
    {
        // Check for repeat handle
        if (hwnd == lastHandle) return;
        lastHandle = hwnd;

        // Ignore ghost window when target is unresponsive
        if (PInvoke.IsHungAppWindow(hwnd)) return;

        // Ignore windows with specific styles
        var windowStyle = PInvokeHelpers.GetWindowStyleEx(hwnd);

        // A top-level window created with this style does not become the foreground window when the user clicks it 
        if (windowStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_NOACTIVATE)) return;

        // Get processId
        _ = PInvokeHelpers.GetWindowThreadProcessId(hwnd, out uint processId);

        // Get process info
        var filename = PInvokeHelpers.GetFilenameForProcess(processId);

        // Explorer tool window for alt-tabbing etc
        if (filename.Equals(explorerPath, StringComparison.CurrentCultureIgnoreCase) && windowStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW)) return;

        // Make args
        var args = new ForegroundWindowChangedEventArgs
        {
            Handle = hwnd,
            ProcessId = processId,
            FileName = filename,
            FromTimer = fromTimer,
            Elapsed = elapsed
        };

        // Get child info from ApplicationFrameHost
        if (Path.GetFileNameWithoutExtension(filename) == "ApplicationFrameHost")
        {
            BOOL lpEnumFunc(HWND hwnd, LPARAM lparam) => FindChildDetails(hwnd, args);
            PInvoke.EnumChildWindows(hwnd, lpEnumFunc, nint.Zero);
        }

        // Send event
        ForegroundWindowChanged?.Invoke(this, args);
    }

    private static bool FindChildDetails(HWND hwnd, ForegroundWindowChangedEventArgs args)
    {
        PInvokeHelpers.GetWindowThreadProcessId(hwnd, out uint processId);
        var filename = PInvokeHelpers.GetFilenameForProcess(processId);
        if (Path.GetFileNameWithoutExtension(filename) != "ApplicationFrameHost")
        {
            args.FileName = filename;
            return false;
        }
        return true;
    }
}
