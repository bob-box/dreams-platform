using Microsoft.AspNetCore.Mvc;

public class ForbiddenResult : ObjectResult
{
    public ForbiddenResult(string message) : base(message)
    {
        StatusCode = StatusCodes.Status403Forbidden;
    }
}