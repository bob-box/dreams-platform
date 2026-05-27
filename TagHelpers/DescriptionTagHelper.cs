using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Borderland.Dreams.TagHelpers;

[HtmlTargetElement("description", Attributes = "asp-for")]
public class DescriptionTagHelper : TagHelper
{
    [HtmlAttributeName("asp-for")]
    public ModelExpression AspFor { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var description = AspFor.Metadata.Description;

        if (string.IsNullOrWhiteSpace(description))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "small";
        output.Attributes.SetAttribute("class", "form-text text-muted d-block");

        output.Content.SetContent(description);
    }
}