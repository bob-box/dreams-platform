using System.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Borderland.Dreams.Extensions;

public static class HtmlHelperExtensions
{
    public static string GetCloudName(this IHtmlHelper htmlHelper)
    {
        return htmlHelper.ViewContext.HttpContext.Request.RouteValues["year"]?.ToString();
    }
}