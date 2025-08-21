namespace Borderland.Dreams.Enums;

public enum DreamStatus
{
    Hidden, // The idea was that this status could only be changed to and away from by only an admin user
    Draft,
    Active,
    PartiallyFunded,
    FullyFunded,
    Canceled
}