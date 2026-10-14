using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers;

[HtmlTargetElement("button", Attributes = "provider")]
public class ProviderButtonTagHelper(IHtmlHelper htmlHelper) : ViewTagHelper(htmlHelper)
{
    public string Provider { get; set; } = "";
    protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
    {
        ViewName = Path.Combine("SignInButtons", Provider);
    }
}

