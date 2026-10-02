using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests;

public class PixelFontTests
{
    private static readonly Rgba32 Black = new(0, 0, 0);
    private static readonly Rgba32 White = new(255, 255, 255);

    [Theory]
    [InlineData("", 1, 0)]
    [InlineData("1", 1, 3)]
    [InlineData("12", 1, 7)]
    [InlineData(":", 1, 1)]
    [InlineData("12:34", 1, 17)]
    [InlineData("12", 2, 14)]
    [InlineData("°C", 1, 6)]
    public void StringWidth_IncludesOnePixelGapsBetweenGlyphs(string text, int scale, int expected) =>
        Assert.Equal(expected, PixelFont.StringWidth(text, scale));

    [Fact]
    public void StringWidth_IsCaseInsensitive() =>
        Assert.Equal(PixelFont.StringWidth("MON"), PixelFont.StringWidth("mon"));

    [Fact]
    public void StringWidth_IgnoresUnknownGlyphs() =>
        Assert.Equal(PixelFont.StringWidth("11"), PixelFont.StringWidth("1Q1"));

    // Every character the renderers can emit must exist in the font, or it silently disappears
    [Theory]
    [InlineData("SUN MON TUE WED THU FRI SAT")]
    [InlineData("JAN FEB MAR APR MAY JUN JUL AUG SEP OCT NOV DEC")]
    [InlineData("0123456789:-°CF")]
    public void Font_CoversAllRenderedCharacters(string text)
    {
        foreach (var c in text.Where(c => c != ' '))
            Assert.True(PixelFont.StringWidth(c.ToString()) > 0, $"Missing glyph '{c}'");
    }

    [Fact]
    public void DrawString_DrawsGlyphBitmapAtPosition()
    {
        using var img = new Image<Rgba32>(32, 32, Black);

        PixelFont.DrawString(img, "4", 2, 3, White);

        Assert.Equal(
            ["#.#", "#.#", "###", "..#", "..#"],
            ImageAssert.Ascii(img, 2, 3, 3, 5, Black));
        Assert.Equal(9, ImageAssert.CountLit(img, Black));
    }

    [Fact]
    public void DrawString_ScalesEachPixelToABlock()
    {
        using var img = new Image<Rgba32>(32, 32, Black);

        PixelFont.DrawString(img, "-", 0, 0, White, scale: 2);

        Assert.Equal((0, 4, 5, 5), ImageAssert.Bounds(img, White));
        Assert.Equal(12, ImageAssert.CountLit(img, Black));
    }

    [Fact]
    public void DrawString_ClipsAtImageEdgesWithoutThrowing()
    {
        using var img = new Image<Rgba32>(32, 32, Black);

        PixelFont.DrawString(img, "88", 29, 29, White, scale: 2);
        PixelFont.DrawString(img, "8", -2, -2, White);

        Assert.True(ImageAssert.CountLit(img, Black) > 0);
    }
}
