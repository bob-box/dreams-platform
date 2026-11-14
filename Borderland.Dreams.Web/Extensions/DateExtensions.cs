namespace Borderland.Dreams.Extensions;

public static partial class Extensions
{
    /// <summary>
    ///     A DateTime extension method that return a DateTime with the time set to "23:59:59:999". The last moment of
    ///     the day. Use "DateTime2" column type in sql to keep the precision.
    /// </summary>
    /// <param name="this">The @this to act on.</param>
    /// <returns>A DateTime of the day with the time set to "23:59:59:999".</returns>
    public static DateTime EndOfDay(this DateTime @this)
    {
        return new DateTime(@this.Year, @this.Month, @this.Day).AddDays(1).Subtract(new TimeSpan(0, 0, 0, 0, 1));
    }

    /// <summary>
    ///     A DateTime extension method that return a DateTime with the time set to "00:00:00:000". The first moment of
    ///     the day. Use "DateTime2" column type in sql to keep the precision.
    /// </summary>
    /// <param name="this">The @this to act on.</param>
    /// <returns>A DateTime of the day with the time set to "00:00:00:000".</returns>
    public static DateTime StartOfDay(this DateTime @this)
    {
        return new DateTime(@this.Year, @this.Month, @this.Day);
    }

    /// <summary>
    /// Determines whether a nullable date falls within a nullable date range.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="start">The start of the range (inclusive by default). If null, no lower bound is applied.</param>
    /// <param name="end">The end of the range (inclusive by default). If null, no upper bound is applied.</param>
    /// <param name="inclusive">
    /// If <c>true</c> (default), the comparison is inclusive of <paramref name="start"/> and <paramref name="end"/>.
    /// If <c>false</c>, the boundaries are excluded.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="date"/> is within the specified range; otherwise <c>false</c>.
    /// Returns <c>true</c> if both <paramref name="start"/> and <paramref name="end"/> are null.
    /// </returns>
    public static bool IsBetween(this DateTime date, DateTime? start, DateTime? end, bool inclusive = true)
    {
        var afterStart = start is null || (inclusive ? date >= start : date > start);
        var beforeEnd = end is null || (inclusive ? date <= end : date < end);

        return afterStart && beforeEnd;
    }
}