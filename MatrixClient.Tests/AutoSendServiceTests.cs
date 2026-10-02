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
        new(new AutoSendConfig { Rules = [.. rules] }, new FakeWebHostEnvironment(_webRoot.Path));

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
    [InlineData("12:00")]
    [InlineData("13:00")]
    public void GetActiveRule_WindowBoundariesAreInclusive(string time)
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "Friday"));

        Assert.NotNull(service.GetActiveRule(Friday + TimeSpan.Parse(time)));
    }

    [Theory]
    [InlineData("11:59")]
    [InlineData("13:01")]
    public void GetActiveRule_ReturnsNullOutsideWindow(string time)
    {
        var service = Create(Rule("Lunch", "12:00", "13:00", "Friday"));

        Assert.Null(service.GetActiveRule(Friday + TimeSpan.Parse(time)));
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
            Rule("Specific", "20:00", "23:59", "Friday"),
            Rule("Every day", "00:00", "23:59", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"));

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
}
