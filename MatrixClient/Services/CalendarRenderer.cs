using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Services;

public static class CalendarRenderer
{
    private static readonly Rgba32 Background = new(12,   4,   0);
    private static readonly Rgba32 DayColor   = new(74, 158, 255);  // blue  — weekday name
    private static readonly Rgba32 NumColor   = new(255, 215,  0);  // gold  — day number
    private static readonly Rgba32 MonthColor = new(110, 110, 110); // grey  — month name

    private static readonly string[] DayNames   = ["SUN","MON","TUE","WED","THU","FRI","SAT"];
    private static readonly string[] MonthNames = ["JAN","FEB","MAR","APR","MAY","JUN",
                                                    "JUL","AUG","SEP","OCT","NOV","DEC"];

    public static Image<Rgba32> Render()
    {
        var now      = DateTime.Now;
        var dayAbbr  = DayNames[(int)now.DayOfWeek];
        var dayNum   = now.Day.ToString();
        var monthAbbr = MonthNames[now.Month - 1];

        var img = new Image<Rgba32>(32, 32, Background);

        // Row 1: weekday name at scale 1 (y=2)
        int w = PixelFont.StringWidth(dayAbbr);
        PixelFont.DrawString(img, dayAbbr, (32 - w) / 2, 2, DayColor);

        // Row 2: day number at scale 2 (y=10), centered
        int numW = PixelFont.StringWidth(dayNum, scale: 2);
        PixelFont.DrawString(img, dayNum, (32 - numW) / 2, 10, NumColor, scale: 2);

        // Row 3: month abbreviation at scale 1 (y=25)
        int mw = PixelFont.StringWidth(monthAbbr);
        PixelFont.DrawString(img, monthAbbr, (32 - mw) / 2, 25, MonthColor);

        return img;
    }
}
