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

    // ── GetImagePath ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("clock")]
    [InlineData("calendar")]
    [InlineData("weather")]
    public void GetImagePath_ReturnsDynamicModesAsIs(string mode) =>
        Assert.Equal(mode, Create().GetImagePath(mode));

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
