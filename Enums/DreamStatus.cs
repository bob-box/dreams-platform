namespace Borderland.Dreams.Enums;

public enum DreamStatus
{
    Hidden, // The idea was that this status could only be changed away from by an admin user
    Draft,
    Active,
    PartiallyFunded,
    FullyFunded,
    Canceled
}