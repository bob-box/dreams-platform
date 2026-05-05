using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Borderland.Dreams.TagHelpers;

[HtmlTargetElement("input", Attributes = "asp-for")]
public class DateTimeLocalTagHelper : TagHelper
{
    // Order > 0 ensures we run AFTER the built-in InputTagHelper (order 0)
    public override int Order => 1;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var modelType = For.ModelExplorer.ModelType;
        var underlyingType = Nullable.GetUnderlyingType(modelType) ?? modelType;

        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            output.Attributes.SetAttribute("title",           "00:00 is start of the day, 23:59 is end of the day.");
            output.Attributes.SetAttribute("data-bs-toggle",  "tooltip");
            output.Attributes.SetAttribute("data-bs-placement","top");
        }
    }
}