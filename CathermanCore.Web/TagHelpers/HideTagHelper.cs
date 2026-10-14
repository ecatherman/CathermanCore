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
        var isStyle = key.Equals("style", StringComparison.OrdinalIgnoreCase);
        var existing = attributes.FirstOrDefault(a => a.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
        var old = existing?.Value?.ToString() ?? "";

        if (existing.HasValue())
            attributes.Remove(existing);

        string merged;
        if (isStyle)
        {
            if (old.Length > 0 && !old.TrimEnd().EndsWith(';'))
                old += ";";

            merged = old + value;

            if (!merged.EndsWith(';'))
                merged += ";";
        }
        else
        {
            merged = old.HasNoValue() ? value : $"{old} {value}";
        }

        attributes.Add(key, merged);
    }
}