using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;

namespace MatrixClient.Tests;

public class AutoSendServiceTests : IDisposable
{
    // 2026-10-02 is a Friday
    private static readonly DateTime Friday = new(2026, 10, 2);

    private readonly TempDir _webRoot = new();

    public void Dispose() => _webRoot.Dispose();

    private AutoSendService Create(params AutoSendRule[] rules) =>
        TestServices.AutoSend(new AutoSendConfig { Rules = [.. rules] }, _webRoot.Path);

    private static AutoSendRule Rule(string name, string from, string to, params string[] days) =>
        new() { Name = name, From = from, To = to, Days = [.. days], Category = "food" };

    // ── GetActiveRule ────────────────────────────────────────────────────────

    [Fact]
    public void GetActiveRule_MatchesDayAndTimeWindow()
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "Friday"));

        Assert.Equal("Lunch", service.GetActiveRule(Friday.AddHours(12.5))?.Name);
    }

    [Theory]
    [InlineData("12:00:00")]
    [InlineData("12:59:59")]
    public void GetActiveRule_FromIsInclusiveToIsExclusive(string time)
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "Friday"));

        Assert.NotNull(service.GetActiveRule(Friday + TimeSpan.Parse(time)));
    }

    [Theory]
    [InlineData("11:59:59")]
    [InlineData("13:00:00")]
    public void GetActiveRule_ReturnsNullOutsideWindow(string time)
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "Friday"));

        Assert.Null(service.GetActiveRule(Friday + TimeSpan.Parse(time)));
    }

    [Fact]
    public void GetActiveRule_EndOfDayCoversLastSecond()
    {
        var service = Create(Rule("Late", "22:00", "24:00", "Friday"));

        Assert.NotNull(service.GetActiveRule(Friday + new TimeSpan(23, 59, 59)));
        Assert.Null(service.GetActiveRule(Friday.AddDays(1)));   // Saturday 00:00
    }

    [Fact]
    public void GetActiveRule_AdjacentRulesHandOverWithoutOverlap()
    {
        var service = Create(
            Rule("Morning", "06:00", "07:00", "Friday"),
            Rule("Breakfast", "07:00", "09:00", "Friday"));

        Assert.Equal("Morning", service.GetActiveRule(Friday + new TimeSpan(6, 59, 59))?.Name);
        Assert.Equal("Breakfast", service.GetActiveRule(Friday.AddHours(7))?.Name);
    }

    [Fact]
    public void GetActiveRule_ReturnsNullOnOtherDays()
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "Friday"));

        Assert.Null(service.GetActiveRule(Friday.AddDays(1).AddHours(12.5)));
    }

    [Fact]
    public void GetActiveRule_DayNamesAreCaseInsensitive()
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "fRiDaY"));

        Assert.NotNull(service.GetActiveRule(Friday.AddHours(12.5)));
    }

    [Fact]
    public void GetActiveRule_FirstMatchingRuleWins()
    {
        var service = Create(
            Rule("Specific", "20:00", "24:00", "Friday"),
            Rule("Every day", "00:00", "24:00", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"));

        Assert.Equal("Specific", service.GetActiveRule(Friday.AddHours(21))?.Name);
        Assert.Equal("Every day", service.GetActiveRule(Friday.AddHours(10))?.Name);
    }

    [Fact]
    public void GetActiveRule_SkipsRulesOutOfSeason()
    {
        var halloween = Rule("Halloween", "17:00", "22:00", "Friday");
        (halloween.FromDate, halloween.ToDate) = ("10-01", "10-31");
        var service = Create(halloween, Rule("Evening", "17:00", "22:00", "Friday"));

        Assert.Equal("Halloween", service.GetActiveRule(Friday.AddHours(18))?.Name);              // Oct 2
        Assert.Equal("Evening", service.GetActiveRule(new DateTime(2026, 7, 3).AddHours(18))?.Name); // a Friday in July
    }

    // ── GetImagePath ─────────────────────────────────────────────────────────

    [Fact]
    public void GetImagePath_ForRule_PicksFromAllItsCategories()
    {
        var dir = Directory.CreateDirectory(Path.Combine(_webRoot.Path, "data", "food")).FullName;
        File.WriteAllBytes(Path.Combine(dir, "apple.png"), []);
        var rule = new AutoSendRule { Name = "Mix", Categories = ["food", "clock"] };

        var picked = Enumerable.Range(0, 50).Select(_ => Create().GetImagePath(rule)).ToHashSet();

        Assert.Equal(new HashSet<string?> { "clock", "food/apple.png" }, picked);
    }

    [Fact]
    public void GetImagePath_ForRule_SkipsEmptyCategories()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot.Path, "data", "empty"));
        var rule = new AutoSendRule { Name = "Mix", Categories = ["empty", "nope", "weather"] };

        Assert.All(Enumerable.Range(0, 20), _ => Assert.Equal("weather", Create().GetImagePath(rule)));
    }

    [Theory]
    [InlineData("clock")]
    [InlineData("calendar")]
    [InlineData("weather")]
    public void GetImagePath_ReturnsDynamicModesAsIs(string mode) =>
        Assert.Equal(mode, Create().GetImagePath(mode));

    [Theory]
    [InlineData(3, 1, "spring")]
    [InlineData(5, 31, "spring")]
    [InlineData(6, 1, "summer")]
    [InlineData(8, 31, "summer")]
    [InlineData(9, 1, "fall")]
    [InlineData(11, 30, "fall")]
    [InlineData(12, 1, "winter")]
    [InlineData(2, 28, "winter")]
    public void SeasonFor_UsesMeteorologicalSeasons(int month, int day, string expected) =>
        Assert.Equal(expected, AutoSendService.SeasonFor(new DateTime(2026, month, day)));

    [Fact]
    public void GetImagePath_SeasonPicksFromTheCurrentSeasonsFolder()
    {
        foreach (var season in AutoSendService.SeasonFolders)
        {
            var dir = Directory.CreateDirectory(Path.Combine(_webRoot.Path, "data", season)).FullName;
            File.WriteAllBytes(Path.Combine(dir, $"{season}_icon.png"), []);
        }
        var october = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));
        var service = TestServices.AutoSend(new AutoSendConfig(), _webRoot.Path, october);

        Assert.Equal("fall/fall_icon.png", service.GetImagePath(AutoSendService.SeasonCategory));
    }

    [Fact]
    public void GetImagePath_ReturnsNullForMissingCategory() =>
        Assert.Null(Create().GetImagePath("nope"));

    [Fact]
    public void GetImagePath_ReturnsNullForEmptyCategory()
    {
        Directory.CreateDirectory(Path.Combine(_webRoot.Path, "data", "empty"));

        Assert.Null(Create().GetImagePath("empty"));
    }

    [Fact]
    public void GetImagePath_PicksFileFromCategory()
    {
        var dir = Directory.CreateDirectory(Path.Combine(_webRoot.Path, "data", "food")).FullName;
        File.WriteAllBytes(Path.Combine(dir, "apple.png"), []);
        File.WriteAllBytes(Path.Combine(dir, "pear.png"), []);

        var picked = Enumerable.Range(0, 50).Select(_ => Create().GetImagePath("food")).ToHashSet();

        Assert.Subset(new HashSet<string?> { "food/apple.png", "food/pear.png" }, picked);
    }

    [Fact]
    public void GetImagePath_IgnoresNonImageAndHiddenFiles()
    {
        var dir = Directory.CreateDirectory(Path.Combine(_webRoot.Path, "data", "food")).FullName;
        File.WriteAllBytes(Path.Combine(dir, "apple.png"), []);
        File.WriteAllBytes(Path.Combine(dir, ".DS_Store"), []);
        File.WriteAllBytes(Path.Combine(dir, "Thumbs.db"), []);
        File.WriteAllBytes(Path.Combine(dir, "notes.txt"), []);

        var picked = Enumerable.Range(0, 50).Select(_ => Create().GetImagePath("food")).ToHashSet();

        Assert.Equal(["food/apple.png"], picked);
    }

    [Fact]
    public void IntervalFor_PrefersRuleOverride()
    {
        var service = TestServices.AutoSend(new AutoSendConfig { IntervalMinutes = 10 }, _webRoot.Path);

        Assert.Equal(1, service.IntervalFor(new AutoSendRule { IntervalMinutes = 1 }));
        Assert.Equal(10, service.IntervalFor(new AutoSendRule()));
    }
}
