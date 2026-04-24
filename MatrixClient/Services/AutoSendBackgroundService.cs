namespace MatrixClient.Services;

public class AutoSendBackgroundService(
    AutoSendService autoSend,
    PixelSenderService sender,
    ILogger<AutoSendBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(autoSend.Config.IntervalMinutes), stoppingToken);

            if (!autoSend.IsEnabled) continue;

            var rule = autoSend.GetActiveRule();
            if (rule is null) continue;

            var path = autoSend.GetRandomImagePath(rule.Category);
            if (path is null)
            {
                logger.LogWarning("Auto-send: no images found in category '{Category}'", rule.Category);
                continue;
            }

            try
            {
                await sender.SendAsync(path);
                logger.LogInformation("Auto-send: sent '{Path}' (rule: {Rule})", path, rule.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-send failed for '{Path}'", path);
            }
        }
    }
}
