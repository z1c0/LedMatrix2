using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests.Helpers;

public static class ImageAssert
{
    // Renders a region as ASCII art: '#' for any pixel that differs from the background
    public static string[] Ascii(Image<Rgba32> img, int x, int y, int w, int h, Rgba32 background)
    {
        var rows = new string[h];
        for (var r = 0; r < h; r++)
        {
            var chars = new char[w];
            for (var c = 0; c < w; c++)
                chars[c] = img[x + c, y + r] == background ? '.' : '#';
            rows[r] = new string(chars);
        }
        return rows;
    }

    public static int CountLit(Image<Rgba32> img, Rgba32 background)
    {
        var n = 0;
        for (var y = 0; y < img.Height; y++)
            for (var x = 0; x < img.Width; x++)
                if (img[x, y] != background) n++;
        return n;
    }

    // Bounding box (inclusive) of all pixels with exactly this colour, or null if absent
    public static (int MinX, int MinY, int MaxX, int MaxY)? Bounds(Image<Rgba32> img, Rgba32 color)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (var y = 0; y < img.Height; y++)
            for (var x = 0; x < img.Width; x++)
                if (img[x, y] == color)
                {
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
        return maxX < 0 ? null : (minX, minY, maxX, maxY);
    }
}

public static class RepoPaths
{
    public static string MatrixClientDir { get; } = FindMatrixClientDir();

    private static string FindMatrixClientDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "MatrixClient");
            if (File.Exists(Path.Combine(candidate, "MatrixClient.csproj")))
                return candidate;
        }
        throw new InvalidOperationException("Could not locate the MatrixClient project directory");
    }
}
