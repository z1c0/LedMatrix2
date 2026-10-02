using System.Net;
using System.Net.Sockets;
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
}

public class TcpPicoProbeTests
{
    [Fact]
    public async Task IsReachable_TrueWhenPortAcceptsConnections()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var probe = new TcpPicoProbe(new PicoConfig { Url = $"http://127.0.0.1:{port}/" });

            Assert.True(await probe.IsReachableAsync(CancellationToken.None));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task IsReachable_FalseWhenNothingListens()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var probe = new TcpPicoProbe(new PicoConfig { Url = $"http://127.0.0.1:{port}/" });

        Assert.False(await probe.IsReachableAsync(CancellationToken.None));
    }
}
