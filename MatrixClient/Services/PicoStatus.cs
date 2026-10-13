namespace MatrixClient.Services;

// Whether the matrix is reachable, and what it last showed so that can be restored after a power cycle
public class PicoStatus(ILogger<PicoStatus> logger, TimeProvider time)
{
    public enum State { Unknown, Online, Offline }

    // Gallery path or dynamic mode when the content came from SendAsync; null for raw drawings
    public record SentContent(string? Path, string Hex);

    // The most recent POST to the matrix, successful or not; Result is the Pico's answer or why there was none
    public record SendAttempt(DateTimeOffset At, SentContent Content, bool Success, string Result, int DurationMs);

    private readonly Lock _lock = new();
    private State _state = State.Unknown;
    private SentContent? _lastSent;
    private SendAttempt? _lastAttempt;

    public State Current { get { lock (_lock) return _state; } }
    public bool IsOffline => Current == State.Offline;
    public SentContent? LastSent { get { lock (_lock) return _lastSent; } }
    public SendAttempt? LastAttempt { get { lock (_lock) return _lastAttempt; } }

    public void ReportAttempt(SentContent content, bool success, string result, TimeSpan duration)
    {
        lock (_lock)
            _lastAttempt = new SendAttempt(time.GetLocalNow(), content, success, result, (int)duration.TotalMilliseconds);
    }

    public void ReportSent(SentContent content)
    {
        lock (_lock)
        {
            _lastSent = content;
            Transition(State.Online);
        }
    }

    public void ReportUnreachable()
    {
        lock (_lock) Transition(State.Offline);
    }

    // Returns true when the matrix wasn't known to be online, i.e. it may have just booted with a blank display
    public bool ReportReachable()
    {
        lock (_lock)
        {
            var wasOnline = _state == State.Online;
            Transition(State.Online);
            return !wasOnline;
        }
    }

    private void Transition(State next)
    {
        if (_state == next) return;
        var previous = _state;
        _state = next;

        // Being off is normal (the matrix is on a presence-switched socket), so log transitions only
        if (next == State.Offline)
            logger.LogInformation("Matrix went offline");
        else if (previous == State.Offline)
            logger.LogInformation("Matrix is back online");
    }
}
