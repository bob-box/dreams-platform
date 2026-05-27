namespace Borderland.Dreams.Interfaces;

public interface IAppTime
{
    DateTime UtcNow { get; }
    DateTime Now { get; }

    /// <summary>
    /// Timezone set in appsettings.json
    /// </summary>
    DateTime ToLocal(DateTime utc);
    DateTime ToUtc(DateTime local);

    /// <summary>
    /// Nullable-aware overloads. If the input is null, the result will be null.
    /// </summary>
    DateTime? ToLocal(DateTime? utc);
    DateTime? ToUtc(DateTime? local);
}