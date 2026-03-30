namespace Borderland.Dreams.Enums;

public enum DreamStatus
{
    // The idea was that the Hidden status could only be changed to and away from by an admin user.
    // Perhaps we could use it as a "deleted" status so we can easily restore dreams that were deleted by mistake?
    Hidden,
    Draft,
    Active,
    PartiallyFunded,
    FullyFunded,
    Canceled
}