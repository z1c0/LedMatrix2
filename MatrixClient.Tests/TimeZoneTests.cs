using System.Net;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatrixClient.Tests;

// The container runs on UTC; everything shown on the matrix or matched by the schedule must use the configured zone
public class TimeZoneTests : IDisposable
{
    private static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    // 08:34 UTC on a Friday in October = 10:34 in Vienna (CEST, UTC+2)
    private static readonly DateTimeOffset SummerUtc = new(2026, 10, 2, 8, 34, 0, TimeSpan.Zero);

    private readonly TempDir _webRoot = new();

    public void Dispose() => _webRoot.Dispose();

    [Fact]
    public void ZonedTimeProvider_ReportsConfiguredZone()
    {
        var provider = new ZonedTimeProvider(Vienna);

        var now = provider.GetLocalNow();

        Assert.Equal(Vienna, provider.LocalTimeZone);
        Assert.Equal(Vienna.GetUtcOffset(now), now.Offset);
    }

    [Theory]
    [InlineData(2026, 7, 1, 2)]    // CEST
    [InlineData(2026, 12, 1, 1)]   // CET
    public void FakeClockInVienna_AppliesDaylightSaving(int y, int m, int d, int offsetHours)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(y, m, d, 12, 0, 0, TimeSpan.Zero)) { Zone = Vienna };

        Assert.Equal(12 + offsetHours, time.GetLocalNow().Hour);
    }

    [Fact]
    public void GetActiveRule_UsesConfiguredZone()
    {
        var rule = new AutoSendRule { Name = "Morning", Days = ["Friday"], From = "10:00", To = "11:00", Category = "clock" };
        var config = new AutoSendConfig { Rules = [rule] };

        var inVienna = TestServices.AutoSend(config, _webRoot.Path, new FakeTimeProvider(SummerUtc) { Zone = Vienna });
        var inUtc    = TestServices.AutoSend(config, _webRoot.Path, new FakeTimeProvider(SummerUtc));

        Assert.Equal("Morning", inVienna.GetActiveRule()?.Name);
        Assert.Null(inUtc.GetActiveRule());
    }

    [Fact]
    public async Task SendAsync_ClockShowsTimeInConfiguredZone()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK);
        var time = new FakeTimeProvider(SummerUtc) { Zone = Vienna };
        var sender = TestServices.Sender(pico, _webRoot.Path, time);

        await sender.SendAsync("clock");

        using var expected = ClockRenderer.Render(new DateTime(2026, 10, 2, 10, 34, 0));
        Assert.Equal(PixelSenderService.ToHex(expected), Assert.Single(pico.Requests).Body);
    }

    [Fact]
    public void AutoSendService_StartsWithConfiguredState()
    {
        Assert.True(TestServices.AutoSend(new AutoSendConfig { EnabledOnStartup = true }, _webRoot.Path).IsEnabled);
        Assert.False(TestServices.AutoSend(new AutoSendConfig(), _webRoot.Path).IsEnabled);
    }
}
