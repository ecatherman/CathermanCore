using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers;

[HtmlTargetElement("*", Attributes = "if")]
public class IfTagHelper : TagHelper
{
    public bool If { get; set; } = true;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (If == false)
        {
            output.SuppressOutput();
        }

        base.Process(context, output);
    }
}
