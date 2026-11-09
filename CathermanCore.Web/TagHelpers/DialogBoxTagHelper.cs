using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers;

public class DialogBoxTagHelper : ViewTagHelper
{
    protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "";
    }
}

