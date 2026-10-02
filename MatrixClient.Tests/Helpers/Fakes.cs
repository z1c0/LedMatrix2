using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace MatrixClient.Tests.Helpers;

public sealed class FakeWebHostEnvironment(string webRootPath) : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = webRootPath;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "MatrixClient";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = webRootPath;
    public string EnvironmentName { get; set; } = "Test";
}

// Records every request and answers with a canned response
public sealed class FakeHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : this((req, _) => Task.FromResult(respond(req))) { }

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public static FakeHttpHandler Returning(HttpStatusCode status, string body = "") =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        lock (Requests) Requests.Add((request, body));
        return await respond(request, ct);
    }
}

public sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

public sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

public sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("matrixclient-tests-").FullName;

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
