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

            if (string.IsNullOrWhiteSpace(rule.Category))
                errors.Add($"{label}: category is required");
            else if (!DynamicImageService.IsDynamic(rule.Category) && !Directory.Exists(Path.Combine(dataRoot, rule.Category)))
                errors.Add($"{label}: unknown category '{rule.Category}'");
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
                if (start >= end || days.Count == 0) continue;

                overlaps.Add($"'{winner.Name}' overrides '{loser.Name}' on {string.Join(", ", days)} " +
                             $"{AutoSendRule.FormatTime(start)}–{AutoSendRule.FormatTime(end)}");
            }
        return overlaps;
    }
}

public class AutoSendRule
{
    private static readonly string[] TimeFormats = [@"h\:mm", @"hh\:mm"];
    private static readonly TimeSpan EndOfDay = TimeSpan.FromDays(1);

    public string Name { get; set; } = "";
    public List<string> Days { get; set; } = [];
    public string From { get; set; } = "00:00";
    public string To { get; set; } = "24:00";   // exclusive; 24:00 means end of day
    public string Category { get; set; } = "";
    public int? IntervalMinutes { get; set; }  // overrides global interval when set

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
