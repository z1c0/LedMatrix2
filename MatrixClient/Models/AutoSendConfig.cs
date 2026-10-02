using System.Globalization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MatrixClient.Models;

public class AutoSendConfig
{
    // Categories that are rendered on the fly instead of read from wwwroot/data
    public static readonly string[] DynamicModes = ["clock", "calendar", "weather"];

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
            if (!toOk)   errors.Add($"{label}: invalid to time '{rule.To}' (expected HH:mm)");
            if (fromOk && toOk && from > to)
                errors.Add($"{label}: from {rule.From} is after to {rule.To} (rules can't wrap past midnight)");

            if (rule.IntervalMinutes is <= 0)
                errors.Add($"{label}: interval_minutes must be positive (got {rule.IntervalMinutes})");

            if (string.IsNullOrWhiteSpace(rule.Category))
                errors.Add($"{label}: category is required");
            else if (!DynamicModes.Contains(rule.Category) && !Directory.Exists(Path.Combine(dataRoot, rule.Category)))
                errors.Add($"{label}: unknown category '{rule.Category}'");
        }

        // Rules are tracked by name, so duplicates would share send timestamps
        foreach (var dup in Rules.GroupBy(r => r.Name).Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key)))
            errors.Add($"rule name '{dup.Key}' is used {dup.Count()} times");

        return errors;
    }
}

public class AutoSendRule
{
    private static readonly string[] TimeFormats = [@"h\:mm", @"hh\:mm"];

    public string Name { get; set; } = "";
    public List<string> Days { get; set; } = [];
    public string From { get; set; } = "00:00";
    public string To { get; set; } = "23:59";
    public string Category { get; set; } = "";
    public int? IntervalMinutes { get; set; }  // overrides global interval when set

    public static bool TryParseTime(string value, out TimeSpan time) =>
        TimeSpan.TryParseExact(value, TimeFormats, CultureInfo.InvariantCulture, out time) &&
        time < TimeSpan.FromDays(1);
}
