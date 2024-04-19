using System.Text.Json.Serialization;

namespace Temetra.Windows;

public struct ClipCursorPadding
{
    [JsonInclude] public int Left;
    [JsonInclude] public int Top;
    [JsonInclude] public int Right;
    [JsonInclude] public int Bottom;

    public ClipCursorPadding() { }

    public ClipCursorPadding(int value) : this(value, value, value, value) { }

    public ClipCursorPadding(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public static readonly ClipCursorPadding Zero = new(0);
}
