using MatrixClient.Models;

namespace MatrixClient.Services;

public interface IPicoProbe
{
    Task<bool> IsReachableAsync(CancellationToken ct);
}

// GET /status: side-effect-free on the Pico (unlike GET /, which fills the display with a random colour).
// Any HTTP answer counts, even a 404 from firmware without that route: what matters is that the server
// responds. A bare TCP connect wasn't enough, since a wedged server still accepted connections.
public class HttpPicoProbe(PicoConfig pico, IHttpClientFactory httpClientFactory) : IPicoProbe
{
    public async Task<bool> IsReachableAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(pico.ProbeTimeoutSeconds));
        try
        {
            using var response = await httpClientFactory.CreateClient().GetAsync(new Uri(new Uri(pico.Url), "status"), cts.Token);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return false;
        }
    }
}
