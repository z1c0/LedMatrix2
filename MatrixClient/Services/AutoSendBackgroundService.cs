namespace MatrixClient.Services;

public class AutoSendBackgroundService(
    AutoSendService autoSend,
    PixelSenderService sender,
    ILogger<AutoSendBackgroundService> logger) : BackgroundService
{
    private readonly Dictionary<string, DateTime> _lastSent = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            if (!autoSend.IsEnabled) continue;

            var rule = autoSend.GetActiveRule();
            if (rule is null) continue;

            int interval = rule.IntervalMinutes ?? autoSend.Config.IntervalMinutes;
            var now = DateTime.Now;

            if (_lastSent.TryGetValue(rule.Name, out var last) &&
                (now - last).TotalMinutes < interval)
                continue;

            var path = autoSend.GetImagePath(rule.Category);
            if (path is null)
            {
                logger.LogWarning("Auto-send: no images found in category '{Category}'", rule.Category);
                continue;
            }

            try
            {
                await sender.SendAsync(path);
                _lastSent[rule.Name] = now;
                logger.LogInformation("Auto-send: sent '{Path}' (rule: {Rule})", path, rule.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-send failed for '{Path}'", path);
            }
        }
    }
}
