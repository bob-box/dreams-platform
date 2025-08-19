using Microsoft.AspNetCore.Mvc.Rendering;

namespace Borderland.Dreams.Extensions;

public static class HtmlHelperExtensions
{
    public static string GetYear(this IHtmlHelper htmlHelper)
    {
        return htmlHelper.ViewContext.HttpContext.Request.RouteValues["year"]?.ToString() ?? string.Empty;
    }
}