using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

internal sealed class SampleViewTagHelper(IHtmlHelper htmlHelper) : ViewTagHelper(htmlHelper)
{
    public bool ProcessViewCalled { get; private set; }
    public string? ViewNameToSet { get; set; }

    protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
    {
        ProcessViewCalled = true;
        if (ViewNameToSet != null)
            ViewName = ViewNameToSet;
    }
}

public class ViewTagHelperTests
{
    [Fact]
    public void Constructor_HtmlHelperNotViewContextAware_Throws()
    {
        var helper = new Mock<IHtmlHelper>(); // deliberately no .As<IViewContextAware>()

        var ex = Assert.Throws<Exception>(() => new SampleViewTagHelper(helper.Object));

        Assert.Contains("IViewContextAware", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_ContextualizesHelperWithViewContext()
    {
        var fixture = new HtmlHelperFixture();
        var viewContext = new ViewContext();
        var tagHelper = new SampleViewTagHelper(fixture.Object) { ViewContext = viewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        fixture.ContextAware.Verify(c => c.Contextualize(viewContext), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_CallsProcessView()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object);

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.True(tagHelper.ProcessViewCalled);
    }

    [Fact]
    public async Task ProcessAsync_NoViewName_UsesClassNameWithoutTagHelperSuffix()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object);

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Equal("Sample", tagHelper.ViewName);
        Assert.Equal("TagHelpers/Sample", Assert.Single(fixture.PartialCalls).ViewName);
    }

    [Fact]
    public async Task ProcessAsync_ViewNameSetInProcessView_IsUsed()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object) { ViewNameToSet = "Custom" };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Equal("TagHelpers/Custom", Assert.Single(fixture.PartialCalls).ViewName);
    }

    [Fact]
    public async Task ProcessAsync_NestedViewName_UsesForwardSlashesOnly()
    {
        // Compiled library views are matched by exact virtual path, so a backslash
        // (what Path.Combine produces on Windows) means the view is never found.
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object) { ViewNameToSet = "SignInButtons/Google" };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        var name = Assert.Single(fixture.PartialCalls).ViewName;
        Assert.Equal("TagHelpers/SignInButtons/Google", name);
        Assert.DoesNotContain('\\', name);
    }

    [Fact]
    public async Task ProcessAsync_PassesTagHelperAsViewModel()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object);

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Same(tagHelper, Assert.Single(fixture.PartialCalls).Model);
    }

    [Fact]
    public async Task ProcessAsync_SetsOutputContentToRenderedPartial()
    {
        var fixture = new HtmlHelperFixture("<p>hello</p>");
        var tagHelper = new SampleViewTagHelper(fixture.Object);
        var output = CreateOutput();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.Equal("<p>hello</p>", output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_InnerHtmlEmpty_IsFilledFromChildContent()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object);

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput(childContent: "inner text"));

        Assert.Equal("inner text", tagHelper.InnerHtml);
    }

    [Fact]
    public async Task ProcessAsync_InnerHtmlAlreadySet_IsNotOverwritten()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper(fixture.Object) { InnerHtml = "preset" };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput(childContent: "other"));

        Assert.Equal("preset", tagHelper.InnerHtml);
    }
}