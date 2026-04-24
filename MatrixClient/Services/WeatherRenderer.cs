using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public static class WeatherRenderer
{
    // 8×8 pixel icons — each byte is one row, MSB = leftmost pixel
    private static readonly byte[] IconSun     = [36, 66, 60, 189, 189, 60, 66, 36];
    private static readonly byte[] IconCloud   = [56, 124, 254, 255, 255, 126, 0, 0];
    private static readonly byte[] IconRain    = [56, 126, 255, 126, 0, 84, 170, 84];
    private static readonly byte[] IconSnow    = [40, 84, 16, 255, 255, 16, 84, 40];
    private static readonly byte[] IconThunder = [56, 124, 255, 24, 48, 24, 32, 0];
    private static readonly byte[] IconFog     = [255, 0, 255, 0, 255, 0, 255, 0];

    private static readonly Rgba32 ColSun     = new(255, 215, 0);
    private static readonly Rgba32 ColCloud   = new(180, 180, 200);
    private static readonly Rgba32 ColRain    = new(80,  140, 255);
    private static readonly Rgba32 ColSnow    = new(200, 230, 255);
    private static readonly Rgba32 ColThunder = new(255, 240, 50);
    private static readonly Rgba32 ColFog     = new(160, 165, 170);
    private static readonly Rgba32 ColUnit    = new(100, 100, 100);

    public static async Task<Image<Rgba32>> RenderAsync(WeatherService weather)
    {
        var data = await weather.GetCurrentAsync();
        var img  = new Image<Rgba32>(32, 32, new Rgba32(0, 0, 16));

        // Icon — centered horizontally, top area (y=1)
        var (icon, iconColor) = GetIcon(data.WeatherCode);
        DrawIcon(img, icon, (32 - 8) / 2, 1, iconColor);

        // Temperature number — scale 2, centered (y=11)
        var tempStr  = ((int)Math.Round(data.Temperature)).ToString();
        var tempColor = TempColor(data.Temperature);
        var tw = PixelFont.StringWidth(tempStr, scale: 2);
        PixelFont.DrawString(img, tempStr, (32 - tw) / 2, 11, tempColor, scale: 2);

        // Unit label — scale 1, centered (y=25)
        var uw = PixelFont.StringWidth(data.Unit);
        PixelFont.DrawString(img, data.Unit, (32 - uw) / 2, 25, ColUnit);

        return img;
    }

    private static (byte[] Icon, Rgba32 Color) GetIcon(int code) => code switch
    {
        0                    => (IconSun,     ColSun),
        1 or 2               => (IconCloud,   new Rgba32(200, 200, 130)),  // partly cloudy: warm grey
        3                    => (IconCloud,   ColCloud),
        45 or 48             => (IconFog,     ColFog),
        >= 51 and <= 67      => (IconRain,    ColRain),
        >= 71 and <= 77      => (IconSnow,    ColSnow),
        >= 80 and <= 82      => (IconRain,    ColRain),
        85 or 86             => (IconSnow,    ColSnow),
        >= 95                => (IconThunder, ColThunder),
        _                    => (IconCloud,   ColCloud),
    };

    private static Rgba32 TempColor(double t) => t switch
    {
        >= 30 => new Rgba32(255, 80,  0),
        >= 20 => new Rgba32(255, 200, 0),
        >= 10 => new Rgba32(0,   220, 100),
        >= 0  => new Rgba32(0,   180, 255),
        _     => new Rgba32(160, 210, 255),
    };

    private static void DrawIcon(Image<Rgba32> img, byte[] rows, int x, int y, Rgba32 color)
    {
        for (int row = 0; row < 8; row++)
            for (int col = 0; col < 8; col++)
                if ((rows[row] & (1 << (7 - col))) != 0)
                {
                    int px = x + col, py = y + row;
                    if ((uint)px < 32 && (uint)py < 32)
                        img[px, py] = color;
                }
    }
}
