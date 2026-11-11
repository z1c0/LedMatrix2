using System.Buffers.Binary;
using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;

namespace MatrixClient.Imaging;

public readonly record struct Rgba32(byte R, byte G, byte B, byte A = 255);

// The file isn't a PNG this app can read
public class ImageFormatException(string message, Exception? inner = null) : Exception(message, inner);

// A small RGBA bitmap: the gallery images, the dynamic renderers and the "last sent" preview.
// PNG is the only file format; PngSharp does the encoding and decoding.
public sealed class PixelImage
{
    private readonly Rgba32[] _pixels;

    public int Width { get; }
    public int Height { get; }

    // Fills with transparent black unless a colour is given
    public PixelImage(int width, int height, Rgba32 fill = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        _pixels = new Rgba32[width * height];
        if (fill != default) Array.Fill(_pixels, fill);
    }

    public Rgba32 this[int x, int y]
    {
        get => _pixels[Index(x, y)];
        set => _pixels[Index(x, y)] = value;
    }

    private int Index(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(x), $"Pixel ({x},{y}) is outside the {Width}x{Height} image");
        return y * Width + x;
    }

    public static async Task<PixelImage> LoadAsync(string path) => Load(await File.ReadAllBytesAsync(path));

    public static PixelImage Load(string path) => Load(File.ReadAllBytes(path));

    public static PixelImage Load(byte[] png)
    {
        CheckStructure(png);
        IRawPng raw;
        try
        {
            raw = Png.DecodeFromByteArray(png);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // PngSharp reports bad input with its own exceptions, but a broken zlib stream or a
            // truncated file surfaces as whatever the stream threw
            throw new ImageFormatException($"Not a readable PNG ({ex.Message})", ex);
        }
        return FromRaw(raw);
    }

    // Far beyond any gallery image (32x32), but small enough that a bogus header can't make us allocate gigabytes
    private const int MaxDimension = 1024;

    // PngSharp 0.6.0 loops forever when the file ends inside an IDAT chunk (Stream.Read returning 0 is
    // never treated as end of file), so every chunk must be complete before it sees the bytes
    private static void CheckStructure(ReadOnlySpan<byte> png)
    {
        ReadOnlySpan<byte> signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        if (!png.StartsWith(signature))
            throw new ImageFormatException("Not a PNG file");

        var pos = signature.Length;
        var first = true;
        while (pos + 12 <= png.Length)   // length, type, CRC
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(png[pos..]);
            var type = png.Slice(pos + 4, 4);
            if (length > (uint)(png.Length - pos - 12))
                throw new ImageFormatException("Truncated PNG");
            if (first)
            {
                if (!type.SequenceEqual("IHDR"u8) || length < 8)
                    throw new ImageFormatException("PNG doesn't start with an IHDR chunk");
                var width = BinaryPrimitives.ReadUInt32BigEndian(png[(pos + 8)..]);
                var height = BinaryPrimitives.ReadUInt32BigEndian(png[(pos + 12)..]);
                if (width is 0 or > MaxDimension || height is 0 or > MaxDimension)
                    throw new ImageFormatException($"Unsupported PNG size {width}x{height}");
                first = false;
            }
            if (type.SequenceEqual("IEND"u8))
                return;
            pos += 12 + (int)length;
        }
        throw new ImageFormatException("Truncated PNG");
    }

    // 8-bit images of every colour type, and palette images of any bit depth (PngSharp unpacks
    // their indices to one byte each). Other bit depths are rejected rather than guessed at.
    private static PixelImage FromRaw(IRawPng raw)
    {
        var ihdr = raw.Ihdr;
        var (type, depth) = (ihdr.ColorType, ihdr.BitDepth);
        if (depth != 8 && type != ColorType.IndexedColor)
            throw new ImageFormatException($"Unsupported PNG: {depth}-bit {type} (only 8-bit or palette images)");

        var image = new PixelImage((int)ihdr.Width, (int)ihdr.Height);
        var data = raw.PixelData;
        var count = image._pixels.Length;
        var bytesPerPixel = type switch
        {
            ColorType.Grayscale or ColorType.IndexedColor => 1,
            ColorType.GrayscaleWithAlpha => 2,
            ColorType.TrueColor => 3,
            ColorType.TrueColorWithAlpha => 4,
            _ => throw new ImageFormatException($"Unsupported PNG colour type {type}"),
        };
        if (data.Length < count * bytesPerPixel)
            throw new ImageFormatException("Truncated PNG pixel data");

        if (type == ColorType.IndexedColor)
        {
            var palette = raw.Plte?.Entries ?? throw new ImageFormatException("Palette PNG without a palette");
            var alpha = raw.Trns?.Data ?? [];
            for (var i = 0; i < count; i++)
            {
                int index = data[i];
                if (index * 3 + 2 >= palette.Length)
                    throw new ImageFormatException($"Palette index {index} out of range");
                image._pixels[i] = new Rgba32(palette[index * 3], palette[index * 3 + 1], palette[index * 3 + 2],
                    index < alpha.Length ? alpha[index] : (byte)255);
            }
            return image;
        }

        for (int i = 0, o = 0; i < count; i++, o += bytesPerPixel)
            image._pixels[i] = type switch
            {
                ColorType.Grayscale          => new Rgba32(data[o], data[o], data[o]),
                ColorType.GrayscaleWithAlpha => new Rgba32(data[o], data[o], data[o], data[o + 1]),
                ColorType.TrueColor          => new Rgba32(data[o], data[o + 1], data[o + 2]),
                _                            => new Rgba32(data[o], data[o + 1], data[o + 2], data[o + 3]),
            };
        return image;
    }

    public byte[] ToPng()
    {
        var rgba = new byte[_pixels.Length * 4];
        for (int i = 0, o = 0; i < _pixels.Length; i++, o += 4)
        {
            var p = _pixels[i];
            (rgba[o], rgba[o + 1], rgba[o + 2], rgba[o + 3]) = (p.R, p.G, p.B, p.A);
        }
        return Png.EncodeToByteArray(Png.CreateRgba(Width, Height, rgba));
    }

    public void SavePng(string path) => File.WriteAllBytes(path, ToPng());
}
