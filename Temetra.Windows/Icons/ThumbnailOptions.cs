namespace Temetra.Windows;

[Flags]
public enum ThumbnailOptions
{
    ResizeToFit = 0x00000000,
    BiggerSizeOk = 0x00000001,
    MemoryOnly = 0x00000002,
    IconOnly = 0x00000004,
    ThumbnailOnly = 0x00000008,
    InCacheOnly = 0x00000010,
    CropToSquare = 0x00000020,
    WideThumbnails = 0x00000040,
    IconBackground = 0x00000080,
    ScaleUp = 0x00000100,
}