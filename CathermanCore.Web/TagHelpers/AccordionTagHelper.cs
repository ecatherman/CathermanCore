using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;
namespace CathermanCore.Web.TagHelpers;

public class AccordionTagHelper : TagHelper
{
    [HtmlAttributeName("id")]
    [Required]
    public string Id { get; set; } = "";
    public string Class { get; set; } = "";
    public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.Add("id", Id);
        Class = $"accordion {Class}";
        output.Attributes.Add("class", Class);

        output.Content = await output.GetChildContentAsync();
    }
}

