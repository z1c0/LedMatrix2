using System.Net.Sockets;
using MatrixClient.Models;

namespace MatrixClient.Services;

public interface IPicoProbe
{
    Task<bool> IsReachableAsync(CancellationToken ct);
}

// A bare TCP connect: unlike GET /, which fills the display with a random colour, it has no visible effect
public class TcpPicoProbe(PicoConfig pico) : IPicoProbe
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(1);

    public async Task<bool> IsReachableAsync(CancellationToken ct)
    {
        var uri = new Uri(pico.Url);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ConnectTimeout);

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(uri.Host, uri.Port, cts.Token);
            return true;
        }
        catch (Exception ex) when (ex is SocketException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return false;
        }
    }
}
