using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public static class ClockRenderer
{
    private static readonly Rgba32 Background = new(0,    8,   24);
    private static readonly Rgba32 DigitColor = new(0,  160,  255);
    private static readonly Rgba32 ColonColor = new(0,   90,  160);

    public static Image<Rgba32> Render()
    {
        var now  = DateTime.Now;
        var hh   = now.ToString("HH");
        var mm   = now.ToString("mm");
        var text = hh + ":" + mm;

        var img = new Image<Rgba32>(32, 32, Background);

        int w = PixelFont.StringWidth(text);
        int x = (32 - w) / 2;
        int y = (32 - PixelFont.GlyphHeight) / 2;

        // Draw hours
        PixelFont.DrawString(img, hh, x, y, DigitColor);
        // Draw colon (offset past the two hour digits)
        int colonX = x + PixelFont.StringWidth(hh) + 1;
        PixelFont.DrawString(img, ":", colonX, y, ColonColor);
        // Draw minutes
        int minX = colonX + PixelFont.StringWidth(":") + 1;
        PixelFont.DrawString(img, mm, minX, y, DigitColor);

        return img;
    }
}
