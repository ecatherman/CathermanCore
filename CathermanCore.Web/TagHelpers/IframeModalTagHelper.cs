using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers
{
    public class IframeModalTagHelper(IHtmlHelper htmlHelper) : ViewTagHelper(htmlHelper)
    {
        public string Id { get; set; } = "";
        protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "";
        }
    }
}

