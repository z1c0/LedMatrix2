using MatrixClient.Models;
using MatrixClient.Tests.Helpers;

namespace MatrixClient.Tests;

public class AutoSendConfigTests : IDisposable
{
    private readonly TempDir _dataRoot = new();

    public AutoSendConfigTests() => Directory.CreateDirectory(Path.Combine(_dataRoot.Path, "food"));

    public void Dispose() => _dataRoot.Dispose();

    private static AutoSendRule ValidRule(string name = "Lunch") =>
        new() { Name = name, Days = ["Friday"], From = "12:00", To = "13:00", Category = "food" };

    private List<string> Validate(params AutoSendRule[] rules) =>
        new AutoSendConfig { Rules = [.. rules] }.Validate(_dataRoot.Path);

    [Theory]
    [InlineData("7:00", 7, 0)]
    [InlineData("07:05", 7, 5)]
    [InlineData("00:00", 0, 0)]
    [InlineData("23:59", 23, 59)]
    [InlineData("24:00", 24, 0)]
    public void TryParseTime_AcceptsHoursAndMinutes(string value, int h, int m)
    {
        Assert.True(AutoSendRule.TryParseTime(value, out var time));
        Assert.Equal(new TimeSpan(h, m, 0), time);
    }

    [Theory]
    [InlineData("24:01")]
    [InlineData("25:00")]
    [InlineData("7:60")]
    [InlineData("7")]
    [InlineData("7:0x")]
    [InlineData("1.07:00")]
    [InlineData("")]
    public void TryParseTime_RejectsInvalidValues(string value) =>
        Assert.False(AutoSendRule.TryParseTime(value, out _));

    [Fact]
    public void Validate_AcceptsValidRules()
    {
        var clock = ValidRule("Clock");
        clock.Category = "clock";

        Assert.Empty(Validate(ValidRule(), clock));
    }

    [Theory]
    [InlineData("Funday", "unknown day 'Funday'")]
    [InlineData("1", "unknown day '1'")]
    public void Validate_RejectsUnknownDays(string day, string expected)
    {
        var rule = ValidRule();
        rule.Days = [day];

        Assert.Contains(Validate(rule), e => e.Contains(expected));
    }

    [Fact]
    public void Validate_RejectsMissingDays()
    {
        var rule = ValidRule();
        rule.Days = [];

        Assert.Contains(Validate(rule), e => e.Contains("no days"));
    }

    [Fact]
    public void Validate_RejectsBadTimes()
    {
        var rule = ValidRule();
        rule.From = "7:0x";
        rule.To = "24:30";

        var errors = Validate(rule);

        Assert.Contains(errors, e => e.Contains("invalid from time '7:0x'"));
        Assert.Contains(errors, e => e.Contains("invalid to time '24:30'"));
    }

    [Fact]
    public void Validate_RejectsWindowsThatWrapPastMidnight()
    {
        var rule = ValidRule();
        rule.From = "22:00";
        rule.To = "02:00";

        Assert.Contains(Validate(rule), e => e.Contains("cross midnight"));
    }

    [Fact]
    public void Validate_RejectsEmptyWindow()
    {
        var rule = ValidRule();
        rule.From = rule.To = "12:00";

        Assert.Contains(Validate(rule), e => e.Contains("must be before"));
    }

    [Fact]
    public void Validate_AcceptsEndOfDay()
    {
        var rule = ValidRule();
        rule.To = "24:00";

        Assert.Empty(Validate(rule));
    }

    [Fact]
    public void DescribeOverlaps_ReportsWinnerSharedDaysAndWindow()
    {
        var gaming = new AutoSendRule { Name = "Gaming", Days = ["Friday", "Saturday"], From = "20:00", To = "24:00", Category = "food" };
        var weather = new AutoSendRule { Name = "Weather", Days = ["Monday", "Friday", "Saturday"], From = "09:00", To = "22:00", Category = "food" };

        var overlaps = new AutoSendConfig { Rules = [gaming, weather] }.DescribeOverlaps();

        Assert.Equal(["'Gaming' overrides 'Weather' on Friday, Saturday 20:00–22:00"], overlaps);
    }

    [Fact]
    public void DescribeOverlaps_IgnoresAdjacentWindowsAndDisjointDays()
    {
        var morning = new AutoSendRule { Name = "Morning", Days = ["Friday"], From = "06:00", To = "07:00" };
        var breakfast = new AutoSendRule { Name = "Breakfast", Days = ["Friday"], From = "07:00", To = "09:00" };
        var weekend = new AutoSendRule { Name = "Weekend", Days = ["Sunday"], From = "06:00", To = "09:00" };

        Assert.Empty(new AutoSendConfig { Rules = [morning, breakfast, weekend] }.DescribeOverlaps());
    }

    [Fact]
    public void Validate_RejectsNonPositiveIntervals()
    {
        var rule = ValidRule();
        rule.IntervalMinutes = 0;
        var config = new AutoSendConfig { IntervalMinutes = -1, Rules = [rule] };

        var errors = config.Validate(_dataRoot.Path);

        Assert.Contains(errors, e => e.StartsWith("interval_minutes must be positive"));
        Assert.Contains(errors, e => e.StartsWith("rule 'Lunch': interval_minutes must be positive"));
    }

    [Theory]
    [InlineData("", "category is required")]
    [InlineData("drinks", "unknown category 'drinks'")]
    public void Validate_RejectsBadCategories(string category, string expected)
    {
        var rule = ValidRule();
        rule.Category = category;

        Assert.Contains(Validate(rule), e => e.Contains(expected));
    }

    [Fact]
    public void Validate_RejectsMissingAndDuplicateNames()
    {
        var errors = Validate(ValidRule(""), ValidRule("Lunch"), ValidRule("Lunch"));

        Assert.Contains("rule #1: name is required", errors);
        Assert.Contains("rule name 'Lunch' is used 2 times", errors);
    }

    [Fact]
    public void FromYaml_ReadsEnabledOnStartup()
    {
        Assert.True(AutoSendConfig.FromYaml("enabled_on_startup: true").EnabledOnStartup);
        Assert.False(AutoSendConfig.FromYaml("interval_minutes: 3").EnabledOnStartup);
    }
}
