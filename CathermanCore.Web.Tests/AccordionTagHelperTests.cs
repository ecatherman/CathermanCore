using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

public class AccordionTagHelperTests
{
    private static string? Attr(TagHelperOutput output, string name) => output.Attributes[name]?.Value?.ToString();

    [Fact]
    public async Task ProcessAsync_RendersDivWithIdAndAccordionClass()
    {
        var output = CreateOutput("accordion", "children");

        await new AccordionTagHelper { Id = "acc1" }.ProcessAsync(CreateContext("accordion"), output);

        Assert.Equal("div", output.TagName);
        Assert.Equal("acc1", Attr(output, "id"));
        Assert.Equal("accordion", Attr(output, "class")?.Trim());
    }

    [Fact]
    public async Task ProcessAsync_CustomClass_IsAppendedAfterAccordion()
    {
        var output = CreateOutput("accordion");

        await new AccordionTagHelper { Id = "acc1", Class = "mt-3" }.ProcessAsync(CreateContext("accordion"), output);

        Assert.Equal("accordion mt-3", Attr(output, "class"));
    }

    [Fact]
    public async Task ProcessAsync_ContentIsChildContent()
    {
        var output = CreateOutput("accordion", "child markup");

        await new AccordionTagHelper { Id = "acc1" }.ProcessAsync(CreateContext("accordion"), output);

        Assert.Equal("child markup", output.Content.GetContent());
    }

    [Fact]
    public void Id_IsBoundToIdAttributeAndRequired()
    {
        var property = typeof(AccordionTagHelper).GetProperty(nameof(AccordionTagHelper.Id))!;

        Assert.Equal("id", property.GetCustomAttribute<HtmlAttributeNameAttribute>()!.Name);
        Assert.NotNull(property.GetCustomAttribute<RequiredAttribute>());
    }
}

public class AccordionItemTagHelperTests
{
    [Fact]
    public async Task ProcessAsync_RendersDivWithAccordionItemClass()
    {
        var fixture = new HtmlHelperFixture();
        var output = CreateOutput("accordion-item");

        await new AccordionItemTagHelper(fixture.Object) { Id = "i1", ParentId = "acc1" }
            .ProcessAsync(CreateContext("accordion-item"), output);

        Assert.Equal("div", output.TagName);
        Assert.Equal("accordion-item", output.Attributes["class"].Value?.ToString()?.Trim());
    }

    [Fact]
    public async Task ProcessAsync_CustomClass_IsAppended()
    {
        var fixture = new HtmlHelperFixture();
        var output = CreateOutput("accordion-item");

        await new AccordionItemTagHelper(fixture.Object) { Id = "i1", ParentId = "acc1", Class = "border-0" }
            .ProcessAsync(CreateContext("accordion-item"), output);

        Assert.Equal("accordion-item border-0", output.Attributes["class"].Value?.ToString());
    }

    [Fact]
    public async Task ProcessAsync_RendersAccordionItemPartialWithItselfAsModel()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new AccordionItemTagHelper(fixture.Object) { Id = "i1", ParentId = "acc1" };

        await tagHelper.ProcessAsync(CreateContext("accordion-item"), CreateOutput("accordion-item"));

        var call = Assert.Single(fixture.PartialCalls);
        Assert.Equal("TagHelpers/AccordionItem", call.ViewName);
        Assert.Same(tagHelper, call.Model);
    }

    [Fact]
    public void Defaults_AreCollapsedWithNoTitle()
    {
        var tagHelper = new AccordionItemTagHelper(new HtmlHelperFixture().Object);

        Assert.False(tagHelper.Expand);
        Assert.Equal("", tagHelper.Title);
        Assert.Equal("", tagHelper.Class);
    }

    [Fact]
    public void IdAndParentId_AreBoundToExpectedAttributesAndRequired()
    {
        var id = typeof(AccordionItemTagHelper).GetProperty(nameof(AccordionItemTagHelper.Id))!;
        var parent = typeof(AccordionItemTagHelper).GetProperty(nameof(AccordionItemTagHelper.ParentId))!;

        Assert.Equal("id", id.GetCustomAttribute<HtmlAttributeNameAttribute>()!.Name);
        Assert.Equal("parent-id", parent.GetCustomAttribute<HtmlAttributeNameAttribute>()!.Name);
        Assert.NotNull(id.GetCustomAttribute<RequiredAttribute>());
        Assert.NotNull(parent.GetCustomAttribute<RequiredAttribute>());
    }
}