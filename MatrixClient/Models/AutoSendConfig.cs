using System.Globalization;
using MatrixClient.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MatrixClient.Models;

public class AutoSendConfig
{
    public int IntervalMinutes { get; set; } = 5;
    public bool EnabledOnStartup { get; set; }
    public List<AutoSendRule> Rules { get; set; } = [];

    public static AutoSendConfig FromYaml(string yaml) =>
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build()
            .Deserialize<AutoSendConfig>(yaml);

    // Returns one message per problem; empty when the config is usable
    public List<string> Validate(string dataRoot)
    {
        var errors = new List<string>();
        if (IntervalMinutes <= 0)
            errors.Add($"interval_minutes must be positive (got {IntervalMinutes})");

        for (var i = 0; i < Rules.Count; i++)
        {
            var rule  = Rules[i];
            var label = string.IsNullOrWhiteSpace(rule.Name) ? $"rule #{i + 1}" : $"rule '{rule.Name}'";

            if (string.IsNullOrWhiteSpace(rule.Name))
                errors.Add($"{label}: name is required");

            if (rule.Days.Count == 0)
                errors.Add($"{label}: no days given");
            foreach (var day in rule.Days.Where(d => !Enum.GetNames<DayOfWeek>().Contains(d, StringComparer.OrdinalIgnoreCase)))
                errors.Add($"{label}: unknown day '{day}'");

            var fromOk = AutoSendRule.TryParseTime(rule.From, out var from);
            var toOk   = AutoSendRule.TryParseTime(rule.To, out var to);
            if (!fromOk) errors.Add($"{label}: invalid from time '{rule.From}' (expected HH:mm)");
            if (!toOk)   errors.Add($"{label}: invalid to time '{rule.To}' (expected HH:mm, or 24:00 for end of day)");
            if (fromOk && toOk && from >= to)
                errors.Add($"{label}: from {rule.From} must be before to {rule.To} (split rules that cross midnight)");

            if (rule.IntervalMinutes is <= 0)
                errors.Add($"{label}: interval_minutes must be positive (got {rule.IntervalMinutes})");

            var hasCategory = !string.IsNullOrWhiteSpace(rule.Category);
            if (hasCategory == rule.Categories.Count > 0)
                errors.Add($"{label}: give either category or categories");
            foreach (var category in rule.AllCategories)
            {
                if (string.IsNullOrWhiteSpace(category))
                    errors.Add($"{label}: categories must not be empty");
                else if (!DynamicImageService.IsDynamic(category) && !Directory.Exists(Path.Combine(dataRoot, category)))
                    errors.Add($"{label}: unknown category '{category}'");
            }

            var fromDateOk = rule.FromDate is null || AutoSendRule.TryParseDate(rule.FromDate, out _);
            var toDateOk   = rule.ToDate is null || AutoSendRule.TryParseDate(rule.ToDate, out _);
            if (!fromDateOk) errors.Add($"{label}: invalid from_date '{rule.FromDate}' (expected MM-dd)");
            if (!toDateOk)   errors.Add($"{label}: invalid to_date '{rule.ToDate}' (expected MM-dd)");
            if ((rule.FromDate is null) != (rule.ToDate is null))
                errors.Add($"{label}: from_date and to_date go together");
        }

        // Rules are tracked by name, so duplicates would share send timestamps
        foreach (var dup in Rules.GroupBy(r => r.Name).Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key)))
            errors.Add($"rule name '{dup.Key}' is used {dup.Count()} times");

        return errors;
    }

    // Where two rules overlap the one listed first wins; describes each case so it's visible, not silent
    public List<string> DescribeOverlaps()
    {
        var overlaps = new List<string>();
        for (var i = 0; i < Rules.Count; i++)
            for (var j = i + 1; j < Rules.Count; j++)
            {
                var (winner, loser) = (Rules[i], Rules[j]);
                if (!winner.TryGetWindow(out var wFrom, out var wTo) || !loser.TryGetWindow(out var lFrom, out var lTo))
                    continue;

                var start = wFrom > lFrom ? wFrom : lFrom;
                var end   = wTo < lTo ? wTo : lTo;
                var days  = winner.Days.Intersect(loser.Days, StringComparer.OrdinalIgnoreCase).ToList();
                if (start >= end || days.Count == 0 || !winner.SharesSeasonWith(loser)) continue;

                var season = winner.Season ?? loser.Season;
                overlaps.Add($"'{winner.Name}' overrides '{loser.Name}' on {string.Join(", ", days)} " +
                             $"{AutoSendRule.FormatTime(start)}–{AutoSendRule.FormatTime(end)}" +
                             (season is null ? "" : $" ({season})"));
            }
        return overlaps;
    }
}

public class AutoSendRule
{
    private static readonly string[] TimeFormats = [@"h\:mm", @"hh\:mm"];
    private static readonly TimeSpan EndOfDay = TimeSpan.FromDays(1);
    private const int LeapYear = 2000;   // so 02-29 parses

    public string Name { get; set; } = "";
    public List<string> Days { get; set; } = [];
    public string From { get; set; } = "00:00";
    public string To { get; set; } = "24:00";   // exclusive; 24:00 means end of day
    public string Category { get; set; } = "";
    public List<string> Categories { get; set; } = [];   // alternative to Category: each send picks one at random
    public int? IntervalMinutes { get; set; }  // overrides global interval when set
    public string? FromDate { get; set; }      // optional season, MM-dd, both ends inclusive;
    public string? ToDate { get; set; }        // may wrap past new year (12-01 to 01-06)

    public IReadOnlyList<string> AllCategories =>
        string.IsNullOrWhiteSpace(Category) ? Categories : [Category];

    public string? Season =>
        TryGetSeason(out var from, out var to)
            ? $"{from.ToString("MMM d", CultureInfo.InvariantCulture)} – {to.ToString("MMM d", CultureInfo.InvariantCulture)}"
            : null;

    public static bool TryParseTime(string value, out TimeSpan time)
    {
        if (value == "24:00")
        {
            time = EndOfDay;
            return true;
        }
        return TimeSpan.TryParseExact(value, TimeFormats, CultureInfo.InvariantCulture, out time) && time < EndOfDay;
    }

    public static string FormatTime(TimeSpan time) => $"{(int)time.TotalHours:D2}:{time.Minutes:D2}";

    public static bool TryParseDate(string value, out DateTime date) =>
        DateTime.TryParseExact($"{LeapYear}-{value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private bool TryGetSeason(out DateTime from, out DateTime to)
    {
        (from, to) = (default, default);
        return FromDate is not null && ToDate is not null &&
               TryParseDate(FromDate, out from) && TryParseDate(ToDate, out to);
    }

    // Year-round when no season is given
    public bool IsInSeason(DateTime date)
    {
        if (!TryGetSeason(out var from, out var to)) return true;
        var day = (date.Month, date.Day);
        var (start, end) = ((from.Month, from.Day), (to.Month, to.Day));
        return start.CompareTo(end) <= 0
            ? day.CompareTo(start) >= 0 && day.CompareTo(end) <= 0
            : day.CompareTo(start) >= 0 || day.CompareTo(end) <= 0;   // wraps past new year
    }

    public bool SharesSeasonWith(AutoSendRule other)
    {
        for (var d = new DateTime(LeapYear, 1, 1); d.Year == LeapYear; d = d.AddDays(1))
            if (IsInSeason(d) && other.IsInSeason(d)) return true;
        return false;
    }

    public bool TryGetWindow(out TimeSpan from, out TimeSpan to)
    {
        to = default;
        return TryParseTime(From, out from) && TryParseTime(To, out to) && from < to;
    }

    public bool IsActiveOn(DayOfWeek day, TimeSpan timeOfDay) =>
        Days.Any(d => string.Equals(d, day.ToString(), StringComparison.OrdinalIgnoreCase)) &&
        TryGetWindow(out var from, out var to) &&
        timeOfDay >= from && timeOfDay < to;
}
