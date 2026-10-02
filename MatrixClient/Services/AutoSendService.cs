using MatrixClient.Models;

namespace MatrixClient.Services;

public class AutoSendService(AutoSendConfig config, GalleryService gallery, TimeProvider time)
{
    private volatile bool _enabled = config.EnabledOnStartup;

    public bool IsEnabled => _enabled;
    public AutoSendConfig Config => config;

    public void Enable()  => _enabled = true;
    public void Disable() => _enabled = false;

    public AutoSendRule? GetActiveRule() => GetActiveRule(time.GetLocalNow().DateTime);

    // First rule (in file order) whose day matches and whose [from, to) window contains now
    public AutoSendRule? GetActiveRule(DateTime now) =>
        config.Rules.FirstOrDefault(r => r.IsActiveOn(now.DayOfWeek, now.TimeOfDay));

    public int IntervalFor(AutoSendRule rule) => rule.IntervalMinutes ?? config.IntervalMinutes;

    public string? GetImagePath(string category)
    {
        // Dynamic modes return themselves as the path; PixelSenderService renders them
        if (DynamicImageService.IsDynamic(category)) return category;

        var images = gallery.GetImages(category);
        return images.Length == 0 ? null : images[Random.Shared.Next(images.Length)];
    }
}
