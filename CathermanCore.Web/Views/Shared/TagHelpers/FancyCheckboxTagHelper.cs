using CathermanCore;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BibleWork.Pages.TagHelpers;

[HtmlTargetElement("fancy-checkbox", TagStructure = TagStructure.NormalOrSelfClosing)]
public class FancyCheckboxTagHelper : TagHelper
{
    public bool Checked { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Class { get; set; } = "";
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "checkbox-wrapper-16");

        if (Id.HasNoValue())
            Id = context.UniqueId;

        if (Name.HasNoValue())
            Name = Id;

        string Label = (await output.GetChildContentAsync()).GetContent();

        string html = Template.Replace("{Label}", Label)
                              .Replace("{Checked}", Checked ? "checked" : "")
                              .Replace("{Id}", Id)
                              .Replace("{Name}", Name)
                              .Replace("{Class}", Class);

        output.Content.SetHtmlContent(html);
    }

    private static readonly string Template = @"
        <label class=""checkbox-wrapper"" for=""{Id}"">
            <input id=""{Id}"" name=""{Name}"" {Checked} type=""checkbox"" class=""checkbox-input {Class}"" / >
            <span class=""checkbox-tile"" >
                <span class=""checkbox-label"" >{Label}</span >
            </span >
        </label >";

    //Source: https://getcssscan.com/css-checkboxes-examples

    //@"<div class=""checkbox-wrapper-16"" >
    //    <label class=""checkbox-wrapper"" >
    //        <input type=""checkbox"" class=""checkbox-input"" / >
    //        <span class=""checkbox-tile"" >
    //            <span class=""checkbox-icon"" >
    //                <svg xmlns=""http://www.w3.org/2000/svg"" width=""192"" height=""192"" fill=""currentColor"" viewBox=""0 0 256 256"" >
    //                <rect width=""256"" height=""256"" fill=""none"" > </rect >
    //                <polygon points=""72 40 184 40 240 104 128 224 16 104 72 40"" fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""12"" > </polygon >
    //                <polygon points=""177.091 104 128 224 78.909 104 128 40 177.091 104"" fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""12"" > </polygon >
    //                <line x1=""16"" y1=""104"" x2=""240"" y2=""104"" fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""12"" > </line >
    //                </svg >
    //            </span >
    //            <span class=""checkbox-label"" > {Label}</span >
    //        </span >
    //    </label >
    //</div >";
}


