namespace Borderland.Dreams.Interfaces;

public interface IMembershipCheckService
{
    Task<bool> IsActiveMember(string email);
}