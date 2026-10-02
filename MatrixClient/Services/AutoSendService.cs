using MatrixClient.Models;

namespace MatrixClient.Services;

public class AutoSendService(AutoSendConfig config, IWebHostEnvironment env, TimeProvider time)
{
    private volatile bool _enabled = config.EnabledOnStartup;

    public bool IsEnabled => _enabled;
    public AutoSendConfig Config => config;

    public void Enable()  => _enabled = true;
    public void Disable() => _enabled = false;

    public AutoSendRule? GetActiveRule() => GetActiveRule(time.GetLocalNow().DateTime);

    public AutoSendRule? GetActiveRule(DateTime now)
    {
        var timeOfDay = now.TimeOfDay;
        return config.Rules.FirstOrDefault(r =>
            r.Days.Any(d => string.Equals(d, now.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase)) &&
            AutoSendRule.TryParseTime(r.From, out var from) && timeOfDay >= from &&
            AutoSendRule.TryParseTime(r.To, out var to) && timeOfDay <= to);
    }

    public string? GetImagePath(string category)
    {
        // Dynamic modes return themselves as the path; PixelSenderService handles them
        if (AutoSendConfig.DynamicModes.Contains(category)) return category;

        var dir = Path.Combine(env.WebRootPath, "data", category);
        if (!Directory.Exists(dir)) return null;
        var files = Directory.GetFiles(dir, "*.*");
        if (files.Length == 0) return null;
        return $"{category}/{Path.GetFileName(files[Random.Shared.Next(files.Length)])}";
    }
}
