namespace MatrixClient.Models;

public class AutoSendConfig
{
    public int IntervalMinutes { get; set; } = 5;
    public List<AutoSendRule> Rules { get; set; } = [];
}

public class AutoSendRule
{
    public string Name { get; set; } = "";
    public List<string> Days { get; set; } = [];
    public string From { get; set; } = "00:00";
    public string To { get; set; } = "23:59";
    public string Category { get; set; } = "";
    public int? IntervalMinutes { get; set; }  // overrides global interval when set
}
