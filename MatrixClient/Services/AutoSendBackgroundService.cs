using MatrixClient.Models;

namespace MatrixClient.Services;

public class AutoSendBackgroundService(
    AutoSendService autoSend,
    PixelSenderService sender,
    PicoStatus status,
    IPicoProbe probe,
    PicoConfig pico,
    TimeProvider time,
    ILogger<AutoSendBackgroundService> logger) : BackgroundService
{
    // Wake slightly after the minute boundary so the clock never renders the previous minute
    private static readonly TimeSpan TickMargin = TimeSpan.FromMilliseconds(200);

    // The tick and probe loops share the per-rule send bookkeeping below
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, DateTime> _lastSent = new();
    private string? _previousRule;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(TickLoopAsync(stoppingToken), ProbeLoopAsync(stoppingToken));

    private async Task TickLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DelayUntilNextMinute(time.GetLocalNow().DateTime), stoppingToken);
            try
            {
                await TickAsync(time.GetLocalNow().DateTime);
            }
            catch (Exception ex)
            {
                // An unhandled exception here would stop the whole host
                logger.LogError(ex, "Auto-send tick failed");
            }
        }
    }

    // The matrix is on a presence-switched socket and boots to a black screen, so poll for it coming back
    private async Task ProbeLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(pico.ProbeSeconds), stoppingToken);
            try
            {
                await ProbeAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Matrix probe failed");
            }
        }
    }

    public static TimeSpan DelayUntilNextMinute(DateTime now) =>
        TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerMinute) + TickMargin;

    public async Task ProbeAsync(CancellationToken ct = default)
    {
        if (!await probe.IsReachableAsync(ct))
        {
            status.ReportUnreachable();
            return;
        }

        if (status.ReportReachable())
            await RestoreAsync(time.GetLocalNow().DateTime);
    }

    public async Task TickAsync(DateTime now)
    {
        await _gate.WaitAsync();
        try
        {
            var rule = autoSend.IsEnabled ? autoSend.GetActiveRule(now) : null;

            // A rule that just became active sends right away instead of waiting out its interval
            var ruleChanged = rule?.Name != _previousRule;
            _previousRule = rule?.Name;
            if (rule is null) return;

            // Nothing to show it on; the probe restores the current content once the matrix is back
            if (status.IsOffline) return;

            var minute = TruncateToMinute(now);
            if (!ruleChanged &&
                _lastSent.TryGetValue(rule.Name, out var last) &&
                (minute - last).TotalMinutes < autoSend.IntervalFor(rule))
                return;

            await SendRuleAsync(rule, minute);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Fills a freshly booted (blank) matrix: the active rule's content, else whatever was shown last
    public async Task RestoreAsync(DateTime now)
    {
        await _gate.WaitAsync();
        try
        {
            var rule = autoSend.IsEnabled ? autoSend.GetActiveRule(now) : null;
            _previousRule = rule?.Name;

            if (rule is not null && await SendRuleAsync(rule, TruncateToMinute(now)))
                return;

            if (status.LastSent is { } last)
            {
                try
                {
                    // Re-render gallery/dynamic content so a restored clock shows the current time
                    if (last.Path is not null)
                        await sender.SendAsync(last.Path);
                    else
                        await sender.SendRawAsync(last.Hex);
                    logger.LogInformation("Restored last content on matrix ({Content})", last.Path ?? "drawing");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Restoring last content failed");
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> SendRuleAsync(AutoSendRule rule, DateTime minute)
    {
        var path = autoSend.GetImagePath(rule);
        if (path is null)
        {
            logger.LogWarning("Auto-send: no images found in '{Categories}'", string.Join(", ", rule.AllCategories));
            return false;
        }

        try
        {
            await sender.SendAsync(path);
            _lastSent[rule.Name] = minute;
            logger.LogInformation("Auto-send: sent '{Path}' (rule: {Rule})", path, rule.Name);
            return true;
        }
        catch (PicoException ex)
        {
            // Expected while the matrix is powered off; PicoStatus logs the transition
            logger.LogDebug(ex, "Auto-send skipped, matrix unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto-send failed for '{Path}'", path);
            return false;
        }
    }

    private static DateTime TruncateToMinute(DateTime t) => new(t.Ticks - t.Ticks % TimeSpan.TicksPerMinute, t.Kind);
}
