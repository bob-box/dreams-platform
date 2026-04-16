using Borderland.Dreams.Interfaces;

namespace Borderland.Dreams.Utils;

public class AppTime(TimeProvider timeProvider, TimeOptions options) : IAppTime
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);

    /// <summary>
    /// Returns the current UTC time. This is the source of truth for all time-related operations in the application.
    /// UTC time is saved in database.
    /// </summary>
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Returns the current time in the configured time zone.
    /// </summary>
    public DateTime Now => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone).DateTime;

    /// <summary>
    /// Converts the given UTC time to the configured time zone.
    /// </summary>
    /// <param name="utc">The UTC time to convert.</param>
    /// <returns>The converted time in the configured time zone.</returns>
    public DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);

    /// <summary>
    /// Converts the given local time to UTC. This is used when saving data to the database, ensuring that all times are stored in UTC regardless of the user's local time zone.
    /// </summary>
    /// <param name="local"></param>
    /// <returns></returns>
    public DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
}