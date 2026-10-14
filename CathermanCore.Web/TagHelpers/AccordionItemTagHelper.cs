using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;

namespace CathermanCore.Web.TagHelpers;

public class AccordionItemTagHelper(IHtmlHelper htmlHelper) : ViewTagHelper(htmlHelper)
{
    [HtmlAttributeName("id")]
    [Required]
    public string Id { get; set; } = "";
    [HtmlAttributeName("parent-id")]
    [Required]
    public string ParentId { get; set; } = "";
    public string Class { get; set; } = "";
    public string Title { get; set; } = "";
    public bool Expand { get; set; } = false;
    protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        Class = $"accordion-item {Class}";
        output.Attributes.Add("class", Class);
    }
}

