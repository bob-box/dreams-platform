using Microsoft.AspNetCore.Mvc.Rendering;

namespace Borderland.Dreams.Extensions;

public static class HtmlHelperExtensions
{
    public static string GetCloudSlug(this IHtmlHelper htmlHelper)
    {
        return htmlHelper.ViewContext.HttpContext.Request.RouteValues["cloudSlug"]?.ToString() ?? string.Empty;
    }
}