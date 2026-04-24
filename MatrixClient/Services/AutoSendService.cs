using MatrixClient.Models;

namespace MatrixClient.Services;

public class AutoSendService(AutoSendConfig config, IWebHostEnvironment env)
{
    private volatile bool _enabled;

    public bool IsEnabled => _enabled;
    public AutoSendConfig Config => config;

    public void Enable()  => _enabled = true;
    public void Disable() => _enabled = false;

    public AutoSendRule? GetActiveRule()
    {
        var now = DateTime.Now;
        var time = now.TimeOfDay;
        return config.Rules.FirstOrDefault(r =>
            r.Days.Any(d => string.Equals(d, now.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase)) &&
            time >= TimeSpan.Parse(r.From) &&
            time <= TimeSpan.Parse(r.To));
    }

    public string? GetRandomImagePath(string category)
    {
        var dir = Path.Combine(env.WebRootPath, "data", category);
        if (!Directory.Exists(dir)) return null;
        var files = Directory.GetFiles(dir, "*.*");
        if (files.Length == 0) return null;
        return $"{category}/{Path.GetFileName(files[Random.Shared.Next(files.Length)])}";
    }
}
