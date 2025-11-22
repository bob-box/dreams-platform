using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

public class AjaxOnlyAttribute : ActionMethodSelectorAttribute 
{
    public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
    {
        return routeContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}