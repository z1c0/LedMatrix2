using System.Net;
using MatrixClient.Models;
using MatrixClient.Services;
using MatrixClient.Tests.Helpers;

namespace MatrixClient.Tests;

public class PicoStatusTests : IDisposable
{
    private readonly TempDir _webRoot = new();

    public void Dispose() => _webRoot.Dispose();

    [Fact]
    public void ReportReachable_IsTrueUnlessAlreadyOnline()
    {
        var status = TestServices.Status();

        Assert.True(status.ReportReachable());    // unknown → online: may have just booted
        Assert.False(status.ReportReachable());   // still online
        status.ReportUnreachable();
        Assert.True(status.IsOffline);
        Assert.True(status.ReportReachable());    // offline → online
    }

    [Fact]
    public async Task SuccessfulSend_RecordsContentAndMarksOnline()
    {
        var status = TestServices.Status();
        status.ReportUnreachable();
        var sender = TestServices.Sender(FakeHttpHandler.Returning(HttpStatusCode.OK), _webRoot.Path, status: status);

        await sender.SendAsync("calendar");

        Assert.Equal(PicoStatus.State.Online, status.Current);
        Assert.Equal("calendar", status.LastSent?.Path);
        Assert.Equal(6144, status.LastSent?.Hex.Length);
    }

    [Fact]
    public async Task Timeout_MarksOffline()
    {
        var status = TestServices.Status();
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = TestServices.Sender(pico, _webRoot.Path, config: new PicoConfig { TimeoutSeconds = 0.1 }, status: status);

        await Assert.ThrowsAsync<PicoException>(() => sender.SendRawAsync("00"));

        Assert.True(status.IsOffline);
        Assert.Null(status.LastSent);
    }

    [Fact]
    public async Task ErrorResponse_DoesNotMarkOffline()
    {
        // The matrix answered, so it's powered; it just rejected this payload
        var status = TestServices.Status();
        var sender = TestServices.Sender(FakeHttpHandler.Returning(HttpStatusCode.BadRequest), _webRoot.Path, status: status);

        await Assert.ThrowsAsync<PicoException>(() => sender.SendRawAsync("00"));

        Assert.False(status.IsOffline);
    }

    // ── Last send attempt (status panel) ─────────────────────────────────────

    [Fact]
    public async Task SuccessfulSend_RecordsAttemptWithPicoAnswerAndTime()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));
        var status = TestServices.Status(time);
        var sender = TestServices.Sender(FakeHttpHandler.Returning(HttpStatusCode.OK, "{ \"result\" : \"OK\" }"), _webRoot.Path, status: status);

        await sender.SendAsync("calendar");

        var attempt = Assert.IsType<PicoStatus.SendAttempt>(status.LastAttempt);
        Assert.True(attempt.Success);
        Assert.Equal("calendar", attempt.Content.Path);
        Assert.Equal("200 OK: { \"result\" : \"OK\" }", attempt.Result);
        Assert.Equal(time.Now, attempt.At);
    }

    [Fact]
    public async Task Timeout_RecordsFailedAttempt()
    {
        var status = TestServices.Status();
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = TestServices.Sender(pico, _webRoot.Path, config: new PicoConfig { TimeoutSeconds = 0.1 }, status: status);

        await Assert.ThrowsAsync<PicoException>(() => sender.SendRawAsync("00"));

        var attempt = Assert.IsType<PicoStatus.SendAttempt>(status.LastAttempt);
        Assert.False(attempt.Success);
        Assert.Equal("No response within 0.1 s", attempt.Result);
        Assert.Null(attempt.Content.Path);
    }

    [Fact]
    public async Task ErrorResponse_RecordsFailedAttemptWithBody()
    {
        var status = TestServices.Status();
        var sender = TestServices.Sender(
            FakeHttpHandler.Returning(HttpStatusCode.BadRequest, "{ \"result\" : \"expected 6144 chars, got 2920\" }"), _webRoot.Path, status: status);

        await Assert.ThrowsAsync<PicoException>(() => sender.SendRawAsync("00"));

        var attempt = Assert.IsType<PicoStatus.SendAttempt>(status.LastAttempt);
        Assert.False(attempt.Success);
        Assert.Equal("400 Bad Request: { \"result\" : \"expected 6144 chars, got 2920\" }", attempt.Result);
    }
}

public class HttpPicoProbeTests
{
    private static HttpPicoProbe Probe(FakeHttpHandler pico, double timeoutSeconds = 5) =>
        new(new PicoConfig { Url = "http://192.168.1.225/", ProbeTimeoutSeconds = timeoutSeconds }, new FakeHttpClientFactory(pico));

    [Fact]
    public async Task IsReachable_AsksForStatus_NotTheRootThatPaintsTheDisplay()
    {
        var pico = FakeHttpHandler.Returning(HttpStatusCode.OK, "{\"result\": \"OK\"}");

        Assert.True(await Probe(pico).IsReachableAsync(CancellationToken.None));

        var request = Assert.Single(pico.Requests).Request;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("http://192.168.1.225/status", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task IsReachable_TrueForAnyHttpAnswer_EvenFromFirmwareWithoutStatusRoute()
    {
        Assert.True(await Probe(FakeHttpHandler.Returning(HttpStatusCode.NotFound)).IsReachableAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IsReachable_FalseWhenTheConnectionFails()
    {
        // What a wedged server looks like: it accepts the connection, then resets it
        var pico = new FakeHttpHandler((_, _) => throw new HttpRequestException("Connection reset by peer"));

        Assert.False(await Probe(pico).IsReachableAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IsReachable_FalseWhenNoAnswerWithinTheTimeout()
    {
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        Assert.False(await Probe(pico, timeoutSeconds: 0.1).IsReachableAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IsReachable_PropagatesShutdown()
    {
        var pico = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var shutdown = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Probe(pico).IsReachableAsync(shutdown.Token));
    }
}
