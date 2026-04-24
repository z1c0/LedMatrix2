using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public static class PixelFont
{
    public const int GlyphHeight = 5;

    // Each glyph: (pixel width, row bit-masks MSB-first left-to-right)
    private static readonly Dictionary<char, (int W, byte[] R)> Glyphs = new()
    {
        ['0'] = (3, [0b111, 0b101, 0b101, 0b101, 0b111]),
        ['1'] = (3, [0b010, 0b110, 0b010, 0b010, 0b111]),
        ['2'] = (3, [0b111, 0b001, 0b111, 0b100, 0b111]),
        ['3'] = (3, [0b111, 0b001, 0b011, 0b001, 0b111]),
        ['4'] = (3, [0b101, 0b101, 0b111, 0b001, 0b001]),
        ['5'] = (3, [0b111, 0b100, 0b111, 0b001, 0b111]),
        ['6'] = (3, [0b111, 0b100, 0b111, 0b101, 0b111]),
        ['7'] = (3, [0b111, 0b001, 0b010, 0b010, 0b010]),
        ['8'] = (3, [0b111, 0b101, 0b111, 0b101, 0b111]),
        ['9'] = (3, [0b111, 0b101, 0b111, 0b001, 0b111]),
        [':'] = (1, [0b0,   0b1,   0b0,   0b1,   0b0  ]),
        [' '] = (3, [0b000, 0b000, 0b000, 0b000, 0b000]),
        ['-'] = (3, [0b000, 0b000, 0b111, 0b000, 0b000]),
        ['°'] = (2, [0b11,  0b11,  0b00,  0b00,  0b00 ]),
        ['A'] = (3, [0b010, 0b101, 0b111, 0b101, 0b101]),
        ['B'] = (3, [0b110, 0b101, 0b110, 0b101, 0b110]),
        ['C'] = (3, [0b111, 0b100, 0b100, 0b100, 0b111]),
        ['D'] = (3, [0b110, 0b101, 0b101, 0b101, 0b110]),
        ['E'] = (3, [0b111, 0b100, 0b111, 0b100, 0b111]),
        ['F'] = (3, [0b111, 0b100, 0b111, 0b100, 0b100]),
        ['G'] = (3, [0b111, 0b100, 0b101, 0b101, 0b111]),
        ['H'] = (3, [0b101, 0b101, 0b111, 0b101, 0b101]),
        ['I'] = (3, [0b111, 0b010, 0b010, 0b010, 0b111]),
        ['J'] = (3, [0b001, 0b001, 0b001, 0b101, 0b111]),
        ['L'] = (3, [0b100, 0b100, 0b100, 0b100, 0b111]),
        ['M'] = (3, [0b101, 0b111, 0b111, 0b101, 0b101]),
        ['N'] = (3, [0b101, 0b111, 0b101, 0b101, 0b101]),
        ['O'] = (3, [0b111, 0b101, 0b101, 0b101, 0b111]),
        ['P'] = (3, [0b111, 0b101, 0b111, 0b100, 0b100]),
        ['R'] = (3, [0b111, 0b101, 0b110, 0b101, 0b101]),
        ['S'] = (3, [0b011, 0b100, 0b010, 0b001, 0b110]),
        ['T'] = (3, [0b111, 0b010, 0b010, 0b010, 0b010]),
        ['U'] = (3, [0b101, 0b101, 0b101, 0b101, 0b111]),
        ['V'] = (3, [0b101, 0b101, 0b101, 0b010, 0b010]),
        ['W'] = (3, [0b101, 0b101, 0b111, 0b111, 0b101]),
        ['Y'] = (3, [0b101, 0b101, 0b111, 0b010, 0b010]),
    };

    // Width of rendered string in pixels (scale applied)
    public static int StringWidth(string text, int scale = 1)
    {
        int total = 0;
        foreach (var c in text.ToUpperInvariant())
            if (Glyphs.TryGetValue(c, out var g))
                total += g.W + 1;
        return total > 0 ? (total - 1) * scale : 0;
    }

    public static void DrawString(Image<Rgba32> img, string text, int x, int y, Rgba32 color, int scale = 1)
    {
        int cx = x;
        foreach (var c in text.ToUpperInvariant())
        {
            if (!Glyphs.TryGetValue(c, out var g)) continue;
            DrawGlyph(img, g.R, cx, y, color, scale, g.W);
            cx += (g.W + 1) * scale;
        }
    }

    private static void DrawGlyph(Image<Rgba32> img, byte[] rows, int x, int y, Rgba32 color, int scale, int w)
    {
        for (int row = 0; row < GlyphHeight; row++)
            for (int col = 0; col < w; col++)
                if ((rows[row] & (1 << (w - 1 - col))) != 0)
                    for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                    {
                        int px = x + col * scale + sx;
                        int py = y + row * scale + sy;
                        if ((uint)px < (uint)img.Width && (uint)py < (uint)img.Height)
                            img[px, py] = color;
                    }
    }
}
