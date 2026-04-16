namespace Borderland.Dreams.Interfaces;

/// <summary>
/// Marker interface to indicate that the implementing entity should have its
/// CreatedAt property automatically populated with the current time when added to the database context.
/// See ApplicationDbContext.SaveChanges() for the implementation that checks 
/// for this interface and sets the CreatedAt property accordingly.
/// </summary>
public interface IAutoPopulateCreatedAtOnAdd
{
    DateTime CreatedAt { get; set; }
}