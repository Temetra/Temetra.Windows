using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Temetra.Windows;

public class ClipCursorHook : IDisposable
{
    private readonly WINEVENTPROC hookDelegate;
    private HWINEVENTHOOK hookInstance;
    private HWND targetHandle;
    private ClipCursorPadding targetPadding;
    private RECT clippedRegion;
    private Timer updateTimer;
    private const int timerDelay = 17;
    private bool disposed = false;

    public ClipCursorHook()
    {
        hookDelegate = new WINEVENTPROC(HookCallback);
        hookInstance = HWINEVENTHOOK.Null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ClipCursorHook()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing) { }
        StopHook();
        disposed = true;
    }

    public void StartHook(nint windowHandle, ClipCursorPadding padding)
    {
        if (!hookInstance.IsNull)
        {
            StopHook();
        }

        if (hookInstance.IsNull)
        {
            targetHandle = (HWND)windowHandle;

            var result = PInvokeHelpers.GetWindowThreadProcessId(targetHandle, out uint processId);
            if (result != 0)
            {
                hookInstance = PInvoke.SetWinEventHook(
                    eventMin: (uint)WindowsEventHookType.EVENT_OBJECT_DESTROY,
                    eventMax: (uint)WindowsEventHookType.EVENT_OBJECT_LOCATIONCHANGE,
                    hmodWinEventProc: HMODULE.Null,
                    pfnWinEventProc: hookDelegate,
                    idProcess: processId,
                    idThread: 0,
                    dwFlags: 0);

                // Save padding and start clip cursor
                this.targetPadding = padding;
                clippedRegion = GetPaddedRegion(targetHandle, padding);
                PInvoke.ClipCursor(clippedRegion);

                // Start timer
                updateTimer = new Timer(TimerCallback, null, 0, timerDelay);
            }
            else
            {
                targetHandle = HWND.Null;
            }
        }
    }

    public void StopHook()
    {
        if (!hookInstance.IsNull)
        {
            updateTimer.Dispose();
            PInvoke.ClipCursor((RECT?)null);
            PInvoke.UnhookWinEvent(hookInstance);
            hookInstance = HWINEVENTHOOK.Null;
            targetHandle = HWND.Null;
        }
    }

    private void TimerCallback(object state)
    {
        // Remove clipped region if the app is not responding
        RECT? target = PInvoke.IsHungAppWindow(targetHandle) 
            ? null 
            : clippedRegion;

        PInvoke.ClipCursor(target);
    }

    private void HookCallback(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // Only process events with valid parameters
        if (hwnd.IsNull || idObject != 0 || hwnd != targetHandle)
        {
            return;
        }

        // Only process relevant events
        var eventTypeEnum = (WindowsEventHookType)eventType;

        if (eventTypeEnum == WindowsEventHookType.EVENT_OBJECT_DESTROY)
        {
            // Window has closed
            StopHook();
        }
        else if (eventTypeEnum == WindowsEventHookType.EVENT_OBJECT_LOCATIONCHANGE)
        {
            // Window has moved/resized
            clippedRegion = GetPaddedRegion(hwnd, targetPadding);
        }
    }

    private static RECT GetPaddedRegion(HWND hwnd, ClipCursorPadding padding)
    {
        PInvoke.GetWindowRect(hwnd, out RECT rect);
        rect.left += padding.Left;
        rect.top += padding.Top;
        rect.right -= padding.Right;
        rect.bottom -= padding.Bottom;
        return rect;
    }
}
