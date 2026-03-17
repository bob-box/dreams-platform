using Microsoft.AspNetCore.Mvc;

namespace Borderland.Dreams.Extensions;

public class ForbiddenResult : ObjectResult
{
    public ForbiddenResult(string message) : base(message)
    {
        StatusCode = StatusCodes.Status403Forbidden;
    }
}