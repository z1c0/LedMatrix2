using System.Net;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatrixClient.Tests;

public class AutoSendBackgroundServiceTests : IDisposable
{
    // 2026-10-02 is a Friday
    private static readonly DateTime TenAm = new(2026, 10, 2, 10, 0, 0);

    private readonly TempDir _webRoot = new();
    private readonly FakeHttpHandler _pico = FakeHttpHandler.Returning(HttpStatusCode.OK);

    public void Dispose() => _webRoot.Dispose();

    private (AutoSendBackgroundService Service, AutoSendService AutoSend) Create(params AutoSendRule[] rules)
    {
        var autoSend = TestServices.AutoSend(new AutoSendConfig { IntervalMinutes = 10, Rules = [.. rules] }, _webRoot.Path);
        var sender = TestServices.Sender(_pico, _webRoot.Path);
        autoSend.Enable();
        return (new AutoSendBackgroundService(autoSend, sender, TimeProvider.System, NullLogger<AutoSendBackgroundService>.Instance), autoSend);
    }

    private static AutoSendRule ClockRule(string name, string from, string to, int? interval = null) =>
        new() { Name = name, From = from, To = to, Days = ["Friday"], Category = "clock", IntervalMinutes = interval };

    [Theory]
    [InlineData("10:00:00",     60.2)]
    [InlineData("10:00:30",     30.2)]
    [InlineData("10:00:59.900", 0.3)]
    public void DelayUntilNextMinute_WakesJustAfterBoundary(string time, double expectedSeconds) =>
        Assert.Equal(expectedSeconds, AutoSendBackgroundService.DelayUntilNextMinute(TenAm.Date + TimeSpan.Parse(time)).TotalSeconds, 3);

    [Fact]
    public async Task Tick_DoesNothingWhenDisabled()
    {
        var (service, autoSend) = Create(ClockRule("Clock", "00:00", "24:00", interval: 1));
        autoSend.Disable();

        await service.TickAsync(TenAm);

        Assert.Empty(_pico.Requests);
    }

    [Fact]
    public async Task Tick_OneMinuteIntervalSurvivesTimerJitter()
    {
        var (service, _) = Create(ClockRule("Clock", "00:00", "24:00", interval: 1));

        // Second tick fires a little "earlier" past its boundary than the first one did
        await service.TickAsync(TenAm.AddMilliseconds(250));
        await service.TickAsync(TenAm.AddMinutes(1).AddMilliseconds(150));
        await service.TickAsync(TenAm.AddMinutes(2).AddMilliseconds(200));

        Assert.Equal(3, _pico.Requests.Count);
    }

    [Fact]
    public async Task Tick_RespectsRuleInterval()
    {
        var (service, _) = Create(ClockRule("Clock", "00:00", "24:00"));   // falls back to global 10 min

        for (var m = 0; m <= 10; m++)
            await service.TickAsync(TenAm.AddMinutes(m));

        Assert.Equal(2, _pico.Requests.Count);   // 10:00 and 10:10
    }

    [Fact]
    public async Task Tick_SendsImmediatelyWhenRuleChanges()
    {
        var (service, _) = Create(
            ClockRule("Morning", "10:00", "10:03"),
            ClockRule("Midday",  "10:03", "12:00"));

        for (var m = 0; m <= 3; m++)
            await service.TickAsync(TenAm.AddMinutes(m));

        Assert.Equal(2, _pico.Requests.Count);   // 10:00 (Morning) and 10:03 (Midday)
    }

    [Fact]
    public async Task Tick_SendsImmediatelyWhenReEnabled()
    {
        var (service, autoSend) = Create(ClockRule("Clock", "00:00", "24:00"));

        await service.TickAsync(TenAm);
        autoSend.Disable();
        await service.TickAsync(TenAm.AddMinutes(1));
        autoSend.Enable();
        await service.TickAsync(TenAm.AddMinutes(2));

        Assert.Equal(2, _pico.Requests.Count);   // 10:00 and 10:02, not held back by the 10 min interval
    }

    [Fact]
    public async Task Tick_RetriesNextMinuteAfterFailedSend()
    {
        var calls = 0;
        var pico = new FakeHttpHandler(_ => new HttpResponseMessage(
            Interlocked.Increment(ref calls) == 1 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        var autoSend = TestServices.AutoSend(
            new AutoSendConfig { IntervalMinutes = 10, Rules = [ClockRule("Clock", "00:00", "24:00")] }, _webRoot.Path);
        var sender = TestServices.Sender(pico, _webRoot.Path);
        autoSend.Enable();
        var service = new AutoSendBackgroundService(autoSend, sender, TimeProvider.System, NullLogger<AutoSendBackgroundService>.Instance);

        await service.TickAsync(TenAm);
        await service.TickAsync(TenAm.AddMinutes(1));

        Assert.Equal(2, calls);
    }
}
