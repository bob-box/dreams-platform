namespace Borderland.Dreams.Enums;

public enum DreamStatus
{
    // The idea was that the Hidden status could only be changed to and away from by an admin user.
    // Perhaps we could use it as a "deleted" status so we can easily restore dreams that were deleted by mistake?
    Hidden = 0,
    Draft = 1,
    ReadyForFunding = 2,
    Active = 3,
    PartiallyFunded = 4, // what is the difference between this and FullyFunded? Maybe we can just have 1 "Funded"?
    FullyFunded = 5,
    Canceled = 6
}