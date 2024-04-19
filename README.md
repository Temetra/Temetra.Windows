# Temetra.Windows

Temetra.Windows is a C# utility library encapsulating some P/Invoke methods.

## Features

Utilities include:

- Foreground window change hook
- Constrain cursor to window service
- Thumbnail provider (including WPF applications)
- Window utilities (bring to front, center and resize)

### ForegroundWindowHook

Watches for foreground window changes, and raises an event when it does. Ignores specific types of windows, such as the hung-application ghost window, and Explorer alt-tab tool windows.

Drills down into ApplicationFrameHost to obtain the actual executable filename.

Must be disposed of, or StopHook() called from, the same thread it was created. See the documentation for [UnhookWinEvent](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwinevent) for more details.

### ClipCursorHook

Restricts the mouse cursor to the area of a window, when given a window handle. ClipCursor is called within a 60 fps timer, and a WinEventHook is used to update the bounds when the target window moves or is resized. The timer is stopped if the window is destroyed.

Must be disposed of, or StopHook() called from, the same thread it was created. See the documentation for [UnhookWinEvent](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwinevent) for more details.

### ThumbnailProvider

Returns a bitmap image for regular files using IShellItemImageFactory. Can return larger icons than obtained from ExtractIconEx.

Can also return a bitmap or image path for a given App Package (appxpackage) path.

### FilteredWindowsEnumerator

Enumerates open windows using [EnumWindows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows), returning executable details for each one. Uses filtering to ignore hidden, cloaked and tool windows. UWP/ApplicationFrameHost processes have their child windows examined to obtain the source executable.

### Tools

BringWindowToFront and CenterAndResizeWindow are helper methods to fill gaps in the WinAppSdk toolset.

## Used By

This project is used by [MouseTrap](https://github.com/Temetra/MouseTrap).

## License

[MIT](https://choosealicense.com/licenses/mit/)
