using MatrixClient.Models;

namespace MatrixClient.Services;

public class AutoSendService(AutoSendConfig config, GalleryService gallery, TimeProvider time)
{
    // Pseudo-category that stands for the current season's gallery folder, so year-round rules can
    // mix in seasonal images without a copy of the rule per season
    public const string SeasonCategory = "season";
    public static readonly string[] SeasonFolders = ["spring", "summer", "fall", "winter"];

    // Meteorological seasons (whole months), which is what people mean by "spring" in Vienna
    public static string SeasonFor(DateTime date) => date.Month switch
    {
        3 or 4 or 5   => "spring",
        6 or 7 or 8   => "summer",
        9 or 10 or 11 => "fall",
        _             => "winter",
    };

    private volatile bool _enabled = config.EnabledOnStartup;

    public bool IsEnabled => _enabled;
    public AutoSendConfig Config => config;

    public void Enable()  => _enabled = true;
    public void Disable() => _enabled = false;

    public AutoSendRule? GetActiveRule() => GetActiveRule(time.GetLocalNow().DateTime);

    // First rule (in file order) that is in season, whose day matches and whose [from, to) window contains now
    public AutoSendRule? GetActiveRule(DateTime now) =>
        config.Rules.FirstOrDefault(r => r.IsInSeason(now) && r.IsActiveOn(now.DayOfWeek, now.TimeOfDay));

    public int IntervalFor(AutoSendRule rule) => rule.IntervalMinutes ?? config.IntervalMinutes;

    // Picks one of the rule's categories at random, skipping any that turn out to be empty
    public string? GetImagePath(AutoSendRule rule) =>
        rule.AllCategories.OrderBy(_ => Random.Shared.Next()).Select(GetImagePath).FirstOrDefault(p => p is not null);

    public string? GetImagePath(string category)
    {
        // Dynamic modes return themselves as the path; PixelSenderService renders them
        if (DynamicImageService.IsDynamic(category)) return category;
        if (category == SeasonCategory) category = SeasonFor(time.GetLocalNow().DateTime);

        var images = gallery.GetImages(category);
        return images.Length == 0 ? null : images[Random.Shared.Next(images.Length)];
    }
}
