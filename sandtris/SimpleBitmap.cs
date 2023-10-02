using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class SimpleBitmap
{
    public int Width { get; }
    public int Height { get; }
    private int[] _pixels;
    Rectangle rect;
    Bitmap _bitmap;
    public SimpleBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new int[width * height];
        rect = new(0, 0, width, height);
        _bitmap = new(width, height);
    }
    public static SimpleBitmap FromBitmap(Bitmap bitmap)
    {
        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            throw new InvalidOperationException("Only supports Format32bppArgb pixel format.");

        int width = bitmap.Width;
        int height = bitmap.Height;

        SimpleBitmap simpleBitmap = new(width, height);

        // Lock the bitmap's bits
        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        // Get the address of the first line
        IntPtr ptr = bmpData.Scan0;

        // Declare an array to hold the bytes of the bitmap
        int bytes = bmpData.Stride * height;
        byte[] rgbValues = new byte[bytes];

        // Copy the RGB values from the bitmap into the array
        Marshal.Copy(ptr, rgbValues, 0, bytes);

        // Copy the byte array values to our integer array
        Buffer.BlockCopy(rgbValues, 0, simpleBitmap._pixels, 0, bytes);

        // Unlock the bits
        bitmap.UnlockBits(bmpData);

        return simpleBitmap;
    }
    public void SetPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException();

        int argb = color.ToArgb();
        _pixels[y * Width + x] = argb;
    }

    public Color GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException();

        return Color.FromArgb(_pixels[y * Width + x]);
    }

    public Bitmap ToBitmap()
    {
        if (_pixels.Length != Width * Height)
            throw new InvalidOperationException("Pixel data doesn't match expected dimensions.");


        // Lock the bitmap's bits
        BitmapData bmpData = _bitmap.LockBits(rect, ImageLockMode.WriteOnly, _bitmap.PixelFormat);

        // Ensure we're working with the expected PixelFormat
        if (bmpData.PixelFormat != PixelFormat.Format32bppArgb)
            throw new InvalidOperationException("Unexpected pixel format.");

        // Get the address of the first line
        IntPtr ptr = bmpData.Scan0;

        if (bmpData.Stride == Width * 4)
        {
            // If the Stride matches the width (common case), we can do a direct copy.
            Marshal.Copy(_pixels, 0, ptr, _pixels.Length);
        }
        else
        {
            // Handle potential stride differences by copying row by row
            for (int i = 0; i < Height; i++)
            {
                nint rowStartPtr = (nint)ptr.ToInt64() + i * bmpData.Stride;
                Marshal.Copy(_pixels, i * Width, rowStartPtr, Width);
            }
        }

        // Unlock the bits
        _bitmap.UnlockBits(bmpData);

        return _bitmap;
    }
}
