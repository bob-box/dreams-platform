using Microsoft.AspNetCore.Mvc.Rendering;

namespace Borderland.Dreams.Extensions;

public static class HtmlHelperExtensions
{
    public static string GetCloudName(this IHtmlHelper htmlHelper)
    {
        return htmlHelper.ViewContext.HttpContext.Request.RouteValues["cloudName"]?.ToString() ?? string.Empty;
    }
}