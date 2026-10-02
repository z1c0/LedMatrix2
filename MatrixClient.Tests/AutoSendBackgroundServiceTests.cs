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
        var rig = CreateRig(_pico, rules);
        return (rig.Service, rig.AutoSend);
    }

    private sealed record Rig(
        AutoSendBackgroundService Service, AutoSendService AutoSend, PixelSenderService Sender,
        PicoStatus Status, FakePicoProbe Probe, FakeTimeProvider Time);

    // Local time == UTC here, starting at Friday 10:00
    private Rig CreateRig(FakeHttpHandler pico, params AutoSendRule[] rules)
    {
        var time = new FakeTimeProvider(new DateTimeOffset(TenAm, TimeSpan.Zero));
        var status = TestServices.Status();
        var probe = new FakePicoProbe();
        var autoSend = TestServices.AutoSend(new AutoSendConfig { IntervalMinutes = 10, Rules = [.. rules] }, _webRoot.Path, time);
        var sender = TestServices.Sender(pico, _webRoot.Path, time, status: status);
        autoSend.Enable();
        var service = new AutoSendBackgroundService(autoSend, sender, status, probe, new PicoConfig(), time,
            NullLogger<AutoSendBackgroundService>.Instance);
        return new Rig(service, autoSend, sender, status, probe, time);
    }

    private static DateTime At(Rig rig, int minutes)
    {
        rig.Time.Now = new DateTimeOffset(TenAm.AddMinutes(minutes), TimeSpan.Zero);
        return TenAm.AddMinutes(minutes);
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
    public async Task Tick_RetriesNextMinuteWhenMatrixRejectsSend()
    {
        var calls = 0;
        var pico = new FakeHttpHandler(_ => new HttpResponseMessage(
            Interlocked.Increment(ref calls) == 1 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        var rig = CreateRig(pico, ClockRule("Clock", "00:00", "24:00"));

        await rig.Service.TickAsync(TenAm);
        await rig.Service.TickAsync(TenAm.AddMinutes(1));

        Assert.Equal(2, calls);
    }

    // ── Offline handling ─────────────────────────────────────────────────────

    [Fact]
    public async Task Tick_UnreachableMatrixGoesOfflineAndIsNotRetriedEveryMinute()
    {
        var pico = new FakeHttpHandler(_ => throw new HttpRequestException("No route to host"));
        var rig = CreateRig(pico, ClockRule("Clock", "00:00", "24:00", interval: 1));

        await rig.Service.TickAsync(TenAm);
        await rig.Service.TickAsync(TenAm.AddMinutes(1));
        await rig.Service.TickAsync(TenAm.AddMinutes(2));

        Assert.True(rig.Status.IsOffline);
        Assert.Single(pico.Requests);
    }

    [Fact]
    public async Task Probe_MarksMatrixOfflineWhenUnreachable()
    {
        var rig = CreateRig(_pico, ClockRule("Clock", "00:00", "24:00"));
        rig.Probe.Reachable = false;

        await rig.Service.ProbeAsync();
        await rig.Service.TickAsync(TenAm);

        Assert.True(rig.Status.IsOffline);
        Assert.Empty(_pico.Requests);
    }

    // Matrix on a presence-switched socket: off at 10:02, back (blank) at 10:05 between two scheduled sends
    [Fact]
    public async Task Probe_RefillsMatrixRightAfterPowerCycleAndRestartsInterval()
    {
        var rig = CreateRig(_pico, ClockRule("Clock", "00:00", "24:00"));   // 10 min interval

        await rig.Service.TickAsync(At(rig, 0));             // scheduled send
        At(rig, 2);
        rig.Probe.Reachable = false;
        await rig.Service.ProbeAsync();                      // power off
        await rig.Service.TickAsync(At(rig, 3));
        At(rig, 5);
        rig.Probe.Reachable = true;
        await rig.Service.ProbeAsync();                      // power on → refill now, not at 10:10
        Assert.Equal(2, _pico.Requests.Count);

        for (var m = 6; m <= 14; m++)
            await rig.Service.TickAsync(At(rig, m));
        Assert.Equal(2, _pico.Requests.Count);              // interval counts from the 10:05 refill

        await rig.Service.TickAsync(At(rig, 15));
        Assert.Equal(3, _pico.Requests.Count);
    }

    [Fact]
    public async Task Probe_FillsMatrixOnFirstContactAfterStartup()
    {
        var rig = CreateRig(_pico, ClockRule("Clock", "00:00", "24:00"));

        await rig.Service.ProbeAsync();
        await rig.Service.TickAsync(At(rig, 1));             // no double send right after

        Assert.Single(_pico.Requests);
    }

    [Fact]
    public async Task Probe_DoesNothingWhileMatrixStaysOnline()
    {
        var rig = CreateRig(_pico, ClockRule("Clock", "00:00", "24:00"));
        await rig.Service.TickAsync(TenAm);

        await rig.Service.ProbeAsync();
        await rig.Service.ProbeAsync();

        Assert.Single(_pico.Requests);
    }

    [Fact]
    public async Task Probe_RestoresLastDrawingWhenNoRuleIsActive()
    {
        var rig = CreateRig(_pico);
        var drawing = string.Concat(Enumerable.Repeat("ABCDEF", 1024));
        await rig.Sender.SendRawAsync(drawing);
        rig.Probe.Reachable = false;
        await rig.Service.ProbeAsync();

        rig.Probe.Reachable = true;
        await rig.Service.ProbeAsync();

        Assert.Equal([drawing, drawing], _pico.Requests.Select(r => r.Body));
    }

    [Fact]
    public async Task Probe_ReRendersLastDynamicContentWhenAutoSendIsOff()
    {
        var rig = CreateRig(_pico, ClockRule("Clock", "00:00", "24:00"));
        rig.AutoSend.Disable();
        await rig.Sender.SendAsync("clock");                 // manual send at 10:00
        rig.Probe.Reachable = false;
        await rig.Service.ProbeAsync();

        At(rig, 7);
        rig.Probe.Reachable = true;
        await rig.Service.ProbeAsync();

        using var expected = ClockRenderer.Render(TenAm.AddMinutes(7));
        Assert.Equal(PixelSenderService.ToHex(expected), _pico.Requests[^1].Body);
    }

    [Fact]
    public async Task Probe_LeavesMatrixAloneWhenThereIsNothingToRestore()
    {
        var rig = CreateRig(_pico);

        await rig.Service.ProbeAsync();

        Assert.Empty(_pico.Requests);
        Assert.False(rig.Status.IsOffline);
    }
}
