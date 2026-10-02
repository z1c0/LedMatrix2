using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests;

public class ClockRendererTests
{
    [Fact]
    public void Render_DrawsCenteredTime()
    {
        using var img = ClockRenderer.Render(new DateTime(2026, 10, 2, 12, 34, 56));
        var bg = img[0, 0];

        // "12:34" is 17px wide and 5px tall → origin (7, 13)
        string[] expected =
        [
            ".#..###...###.#.#",
            "##....#.#...#.#.#",
            ".#..###....##.###",
            ".#..#...#...#...#",
            "###.###...###...#",
        ];
        Assert.Equal(expected, ImageAssert.Ascii(img, 7, 13, 17, 5, bg));
        Assert.Equal(expected.Sum(r => r.Count(c => c == '#')), ImageAssert.CountLit(img, bg));
    }

    [Fact]
    public void Render_UsesTwentyFourHourFormat()
    {
        using var morning = ClockRenderer.Render(new DateTime(2026, 10, 2, 9, 5, 0));
        using var evening = ClockRenderer.Render(new DateTime(2026, 10, 2, 21, 5, 0));
        var bg = morning[0, 0];

        // Leading zero for single-digit hours: first hour digit of "09:05" is '0'
        Assert.Equal(["###", "#.#", "#.#", "#.#", "###"], ImageAssert.Ascii(morning, 7, 13, 3, 5, bg));
        // No 12h wrap-around: second hour digit of "21:05" is '1', not '9'
        Assert.Equal([".#.", "##.", ".#.", ".#.", "###"], ImageAssert.Ascii(evening, 11, 13, 3, 5, bg));
    }
}

public class CalendarRendererTests
{
    [Fact]
    public void Render_DrawsWeekdayDayNumberAndMonthInThreeRows()
    {
        // 2026-10-02 is a Friday
        using var img = CalendarRenderer.Render(new DateTime(2026, 10, 2));

        var dayColor   = img[10, 2];   // top-left pixel of 'F'
        var numColor   = img[13, 10];  // top-left pixel of '2' at scale 2
        var monthColor = img[10, 25];  // top-left pixel of 'O'

        Assert.Equal((10, 2, 20, 6), ImageAssert.Bounds(img, dayColor));      // "FRI", 11px wide
        Assert.Equal((13, 10, 18, 19), ImageAssert.Bounds(img, numColor));    // "2", 6px wide at scale 2
        Assert.Equal((10, 25, 20, 29), ImageAssert.Bounds(img, monthColor));  // "OCT", 11px wide
        Assert.Equal(3, new[] { dayColor, numColor, monthColor }.Distinct().Count());
    }

    [Fact]
    public void Render_CentersTwoDigitDays()
    {
        using var img = CalendarRenderer.Render(new DateTime(2026, 10, 28));
        var numColor = img[9, 10];

        // "28" is 14px wide at scale 2 → x 9..22
        Assert.Equal((9, 10, 22, 19), ImageAssert.Bounds(img, numColor));
    }

    [Fact]
    public void Render_HandlesEveryDayOfTheYear()
    {
        for (var d = new DateTime(2026, 1, 1); d.Year == 2026; d = d.AddDays(1))
        {
            using var img = CalendarRenderer.Render(d);
            Assert.True(ImageAssert.CountLit(img, img[0, 0]) > 0);
        }
    }
}

public class WeatherRendererTests
{
    private static readonly Rgba32 RainColor = new(80, 140, 255);
    private static readonly Rgba32 FreezingColor = new(160, 210, 255);
    private static readonly Rgba32 UnitColor = new(100, 100, 100);

    [Fact]
    public void Render_DrawsIconTemperatureAndUnit()
    {
        using var img = WeatherRenderer.Render(new WeatherService.WeatherData(-5.4, 61, "°C"));

        Assert.Equal((12, 1, 19, 8), ImageAssert.Bounds(img, RainColor));       // 8×8 icon, centered
        Assert.Equal((9, 11, 22, 20), ImageAssert.Bounds(img, FreezingColor));  // "-5" at scale 2
        Assert.Equal((13, 25, 18, 29), ImageAssert.Bounds(img, UnitColor));     // "°C"
    }

    [Theory]
    [InlineData(0,  255, 215, 0)]    // clear → sun
    [InlineData(3,  180, 180, 200)]  // overcast → cloud
    [InlineData(45, 160, 165, 170)]  // fog
    [InlineData(63, 80,  140, 255)]  // rain
    [InlineData(73, 200, 230, 255)]  // snow
    [InlineData(81, 80,  140, 255)]  // rain showers
    [InlineData(95, 255, 240, 50)]   // thunderstorm
    [InlineData(10, 180, 180, 200)]  // unmapped → cloud
    public void Render_PicksIconForWeatherCode(int code, byte r, byte g, byte b)
    {
        using var img = WeatherRenderer.Render(new WeatherService.WeatherData(-20, code, "°C"));

        var bounds = ImageAssert.Bounds(img, new Rgba32(r, g, b));
        Assert.NotNull(bounds);
        Assert.InRange(bounds.Value.MaxY, 1, 8);
    }

    [Theory]
    [InlineData(35,  255, 80,  0)]
    [InlineData(25,  255, 200, 0)]
    [InlineData(15,  0,   220, 100)]
    [InlineData(5,   0,   180, 255)]
    [InlineData(-1,  160, 210, 255)]
    public void Render_ColorsTemperatureByRange(double temp, byte r, byte g, byte b)
    {
        using var img = WeatherRenderer.Render(new WeatherService.WeatherData(temp, 45, "°C"));

        var bounds = ImageAssert.Bounds(img, new Rgba32(r, g, b));
        Assert.NotNull(bounds);
        Assert.Equal(11, bounds.Value.MinY);
    }

    [Fact]
    public void Render_RoundsTemperature()
    {
        using var rounded = WeatherRenderer.Render(new WeatherService.WeatherData(4.6, 45, "°C"));
        using var exact   = WeatherRenderer.Render(new WeatherService.WeatherData(5, 45, "°C"));

        Assert.Equal(
            ImageAssert.Ascii(exact, 0, 0, 32, 32, exact[0, 0]),
            ImageAssert.Ascii(rounded, 0, 0, 32, 32, rounded[0, 0]));
    }
}
