using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers;

public class ModalTagHelper(IHtmlHelper htmlHelper) : ViewTagHelper(htmlHelper)
{
    public string Header { get; set; } = "";
    public string Id { get; set; } = "";
    public bool Write { get; set; } = false;

    public string CustomCallback { get; set; } = "";

    public string CustomText { get; set; } = "Delete";

    public string Size { get; set; } = "";

    protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "";
    }
}

