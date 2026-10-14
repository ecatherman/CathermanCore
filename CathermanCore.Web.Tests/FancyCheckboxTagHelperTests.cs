using BibleWork.Pages.TagHelpers;
using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

public class FancyCheckboxTagHelperTests
{
    private static async Task<(TagHelperOutput Output, string Html)> Render(
        FancyCheckboxTagHelper tagHelper, string label = "Label", string uniqueId = "unique-id")
    {
        var output = CreateOutput("fancy-checkbox", label);
        await tagHelper.ProcessAsync(CreateContext("fancy-checkbox", uniqueId: uniqueId), output);
        return (output, output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_RendersWrapperDiv()
    {
        var (output, _) = await Render(new FancyCheckboxTagHelper());

        Assert.Equal("div", output.TagName);
        Assert.Equal("checkbox-wrapper-16", output.Attributes["class"].Value?.ToString());
    }

    [Fact]
    public async Task ProcessAsync_NoId_UsesContextUniqueIdForIdAndName()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper(), uniqueId: "uid123");

        Assert.Contains("id=\"uid123\"", html);
        Assert.Contains("name=\"uid123\"", html);
        Assert.Contains("for=\"uid123\"", html);
    }

    [Fact]
    public async Task ProcessAsync_IdWithoutName_NameDefaultsToId()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper { Id = "chk" });

        Assert.Contains("id=\"chk\"", html);
        Assert.Contains("name=\"chk\"", html);
    }

    [Fact]
    public async Task ProcessAsync_IdAndName_AreBothUsed()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper { Id = "chk", Name = "Selected" });

        Assert.Contains("id=\"chk\"", html);
        Assert.Contains("name=\"Selected\"", html);
    }

    [Fact]
    public async Task ProcessAsync_Checked_RendersCheckedAttribute()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper { Checked = true });

        Assert.Contains("checked", html);
    }

    [Fact]
    public async Task ProcessAsync_NotChecked_DoesNotRenderCheckedAttribute()
    {
        // None of the template's class names contain the word "checked".
        var (_, html) = await Render(new FancyCheckboxTagHelper { Checked = false });

        Assert.DoesNotContain("checked", html);
    }

    [Fact]
    public async Task ProcessAsync_ChildContentBecomesLabel()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper(), label: "Include notes");

        Assert.Contains("Include notes", html);
    }

    [Fact]
    public async Task ProcessAsync_CustomClass_IsAddedToInput()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper { Class = "extra" });

        Assert.Contains("checkbox-input extra", html);
    }

    [Fact]
    public async Task ProcessAsync_RendersCheckboxInput()
    {
        var (_, html) = await Render(new FancyCheckboxTagHelper());

        Assert.Contains("type=\"checkbox\"", html);
    }
}