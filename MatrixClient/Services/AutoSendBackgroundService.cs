namespace MatrixClient.Services;

public class AutoSendBackgroundService(
    AutoSendService autoSend,
    PixelSenderService sender,
    ILogger<AutoSendBackgroundService> logger) : BackgroundService
{
    // Wake slightly after the minute boundary so the clock never renders the previous minute
    private static readonly TimeSpan TickMargin = TimeSpan.FromMilliseconds(200);

    private readonly Dictionary<string, DateTime> _lastSent = new();
    private string? _previousRule;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DelayUntilNextMinute(DateTime.Now), stoppingToken);
            await TickAsync(DateTime.Now);
        }
    }

    public static TimeSpan DelayUntilNextMinute(DateTime now) =>
        TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerMinute) + TickMargin;

    public async Task TickAsync(DateTime now)
    {
        var rule = autoSend.IsEnabled ? autoSend.GetActiveRule(now) : null;

        // A rule that just became active sends right away instead of waiting out its interval
        var ruleChanged = rule?.Name != _previousRule;
        _previousRule = rule?.Name;
        if (rule is null) return;

        // Compare whole minutes so tick jitter around the boundary can't skip a send
        var minute   = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMinute, now.Kind);
        int interval = rule.IntervalMinutes ?? autoSend.Config.IntervalMinutes;

        if (!ruleChanged &&
            _lastSent.TryGetValue(rule.Name, out var last) &&
            (minute - last).TotalMinutes < interval)
            return;

        var path = autoSend.GetImagePath(rule.Category);
        if (path is null)
        {
            logger.LogWarning("Auto-send: no images found in category '{Category}'", rule.Category);
            return;
        }

        try
        {
            await sender.SendAsync(path);
            _lastSent[rule.Name] = minute;
            logger.LogInformation("Auto-send: sent '{Path}' (rule: {Rule})", path, rule.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto-send failed for '{Path}'", path);
        }
    }
}
