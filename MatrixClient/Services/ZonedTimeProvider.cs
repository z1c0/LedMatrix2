namespace MatrixClient.Services;

// System clock with an explicit local time zone, so clock/calendar/schedule don't depend on the host's TZ
public sealed class ZonedTimeProvider(TimeZoneInfo zone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => zone;
}
