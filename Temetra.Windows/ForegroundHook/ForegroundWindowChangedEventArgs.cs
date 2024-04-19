namespace Temetra.Windows;

public class ForegroundWindowChangedEventArgs : EventArgs
{
    public IntPtr Handle { get; set; }
    public uint ProcessId { get; set; }
    public string FileName { get; set; }
    public bool FromTimer { get; set; }
    public uint Elapsed { get; set; }
}
