using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Temetra.Windows;

public partial class ThumbnailProvider
{
    [GeneratedRegex(@".*(scale|targetsize)-[0-9]+\.png")]
    private static partial Regex ImageNameRegex();

    private static readonly string IShellItem2Guid = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

    public static string GetPackageThumbnailPath(string path, string image)
    {
        // Get component parts of filename
        var imageExt = Path.GetExtension(image);
        var imageName = Path.GetFileNameWithoutExtension(image);

        // Look for scaled images
        var enumPath = Path.Combine(path, Path.GetDirectoryName(image));

        if (Path.Exists(enumPath))
        {
            var files = Directory.EnumerateFiles(enumPath, $"{imageName}*{imageExt}")
                .Where(x => ImageNameRegex().IsMatch(x))
                .Order(new ImageFilenameComparer());

            // Use largest scaled image, or use the given image path if none
            return files.Any() ? files.Last() : Path.Combine(path, image);
        }
        else
        {
            return null;
        }
    }

    public static Bitmap GetPackageThumbnail(string path, string image)
    {
        var thumbnailPath = GetPackageThumbnailPath(path, image);

        if (Path.Exists(thumbnailPath))
        {
            return new Bitmap(thumbnailPath);
        }
        else
        {
            return null;
        }
    }

    public static Bitmap GetThumbnail(string filename, int width, int height, ThumbnailOptions options)
    {
        SIIGBF mappedOptions = (SIIGBF)options;
        var bitmap = GetBitmapFromHbitmap(filename, width, height, mappedOptions);

        if (Image.GetPixelFormatSize(bitmap.PixelFormat) < 32)
        {
            return bitmap;
        }
        else
        {
            using (bitmap)
            {
                return CreateAlphaBitmap(bitmap, PixelFormat.Format32bppArgb);
            }
        }
    }

    private static Bitmap GetBitmapFromHbitmap(string filename, int width, int height, SIIGBF options)
    {
        // Get IShellItem
        Guid shellItem2Guid = new(IShellItem2Guid);
        HRESULT hr = PInvoke.SHCreateItemFromParsingName(filename, null, in shellItem2Guid, out object nativeShellItem);
        if (hr != 0)
        {
            throw new ThumbnailFactoryException($"Failed to create IShellItem (HRESULT {hr})");
        }

        // Get handle to GDI bitmap
        DeleteObjectSafeHandle hBitmap;
        var size = new SIZE(width, height);
        try
        {
            ((IShellItemImageFactory)nativeShellItem).GetImage(size, options, out hBitmap);
        }
        catch (COMException ex)
        {
            throw new ThumbnailFactoryException($"Failed to get image using IShellItemImageFactory (HRESULT {ex.HResult})");
        }

        // Create bitmap from handle
        using (hBitmap)
        {
            return Image.FromHbitmap(hBitmap.DangerousGetHandle());
        }
    }

    private static unsafe Bitmap CreateAlphaBitmap(Bitmap srcBitmap, PixelFormat targetPixelFormat)
    {
        var result = new Bitmap(srcBitmap.Width, srcBitmap.Height, targetPixelFormat);
        var bmpBounds = new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height);
        var srcData = srcBitmap.LockBits(bmpBounds, ImageLockMode.ReadOnly, srcBitmap.PixelFormat);
        var destData = result.LockBits(bmpBounds, ImageLockMode.ReadOnly, targetPixelFormat);
        var srcDataPtr = srcData.Scan0;
        var destDataPtr = destData.Scan0;

        try
        {
            for (int y = 0; y <= srcData.Height - 1; y++)
            {
                for (int x = 0; x <= srcData.Width - 1; x++)
                {
                    //this is really important because one stride may be positive and the other negative
                    var position = srcData.Stride * y + 4 * x;
                    var position2 = destData.Stride * y + 4 * x;
                    Memcpy(destDataPtr + position2, srcDataPtr + position, 4);
                }
            }
        }
        finally
        {
            srcBitmap.UnlockBits(srcData);
            result.UnlockBits(destData);
        }

        return result;
    }

    private static unsafe void Memcpy(IntPtr dest, IntPtr src, int size)
    {
        var srcSpan = new Span<byte>(src.ToPointer(), size);
        var dstSpan = new Span<byte>(dest.ToPointer(), size);
        srcSpan.CopyTo(dstSpan);
    }
}