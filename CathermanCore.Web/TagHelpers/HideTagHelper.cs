using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers;

[HtmlTargetElement("*", Attributes = "hide")]
public class HideTagHelper : TagHelper
{
    public bool Hide { get; set; } = true;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Hide)
            output.Attributes.AddOrAppend("style", "display: none;");

        base.Process(context, output);
    }
}

[HtmlTargetElement("*", Attributes = "show")]
public class ShowTagHelper : TagHelper
{
    public bool Show { get; set; } = true;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Show)
            output.Attributes.AddOrAppend("style", "display: none;");

        base.Process(context, output);
    }
}

public static class TagHelperExtensions
{
    public static void AddOrAppend(this TagHelperAttributeList attributes, string key, string value)
    {
        if(!value.EndsWith(';'))
            value = $"{value};";

        var attribute = attributes.Where(a => a.Name == "style").FirstOrDefault();
        if( attribute != null)
        { 
            attributes.Remove(attribute);
            value = $"{value}{attribute.Value}";

            if (!value.EndsWith(';'))
                value = $"{value};";
        }
        
        attributes.Add(key, value);
    }
}