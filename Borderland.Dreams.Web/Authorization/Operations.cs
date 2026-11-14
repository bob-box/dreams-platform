using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Borderland.Dreams.Authorization;

public static class Operations
{
    public static OperationAuthorizationRequirement Create = new() { Name = Constants.CREATE };
    public static OperationAuthorizationRequirement Read = new() { Name = Constants.READ };
    public static OperationAuthorizationRequirement Update = new() { Name = Constants.UPDATE };
    public static OperationAuthorizationRequirement Delete = new() { Name = Constants.DELETE };
    public static OperationAuthorizationRequirement EditDreamers = new() { Name = Constants.EDIT_DREAMER };
    public static OperationAuthorizationRequirement Publish = new() { Name = Constants.PUBLISH };
    public static OperationAuthorizationRequirement ReturnToDraft = new() { Name = Constants.RETURN_TO_DRAFT };
    public static OperationAuthorizationRequirement Cancel = new() { Name = Constants.CANCEL };
}