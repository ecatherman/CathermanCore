using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

internal sealed class SampleViewTagHelper : ViewTagHelper
{
    public bool ProcessViewCalled { get; private set; }
    public string? ViewNameToSet { get; set; }

    public T Resolve<T>() where T : notnull => GetService<T>();

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
    public async Task ProcessAsync_HtmlHelperNotViewContextAware_Throws()
    {
        var helper = new Mock<IHtmlHelper>(); // deliberately no .As<IViewContextAware>()
        var tagHelper = new SampleViewTagHelper
        {
            ViewContext = CreateViewContext(s => s.AddSingleton(helper.Object))
        };

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            tagHelper.ProcessAsync(CreateContext(), CreateOutput()));

        Assert.Contains("IViewContextAware", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_HtmlHelperNotRegistered_Throws()
    {
        var tagHelper = new SampleViewTagHelper { ViewContext = CreateViewContext() };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tagHelper.ProcessAsync(CreateContext(), CreateOutput()));
    }

    [Fact]
    public async Task ProcessAsync_ResolvesHelperFromRequestServices()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        // The only way this helper can have been used is by being fetched from RequestServices.
        Assert.Single(fixture.PartialCalls);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesAFreshHelperOnEveryCall()
    {
        // IHtmlHelper is registered transient by MVC, and a helper is only valid for the
        // ViewContext it was contextualized with, so each render must fetch its own.
        var fixtures = new List<HtmlHelperFixture>();
        var viewContext = CreateViewContext(s => s.AddTransient<IHtmlHelper>(_ =>
        {
            var created = new HtmlHelperFixture();
            fixtures.Add(created);
            return created.Object;
        }));
        var tagHelper = new SampleViewTagHelper { ViewContext = viewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());
        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Equal(2, fixtures.Count);
        Assert.All(fixtures, f => Assert.Single(f.PartialCalls));
    }

    [Fact]
    public async Task ProcessAsync_ContextualizesHelperWithViewContext()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        fixture.ContextAware.Verify(c => c.Contextualize(fixture.ViewContext), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ContextualizesBeforeRenderingThePartial()
    {
        var fixture = new HtmlHelperFixture();
        var order = new List<string>();
        fixture.ContextAware.Setup(c => c.Contextualize(It.IsAny<ViewContext>()))
            .Callback(() => order.Add("contextualize"));
        fixture.Mock.Setup(h => h.PartialAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
            .Callback(() => order.Add("partial"))
            .ReturnsAsync((IHtmlContent)new HtmlString(""));
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Equal(new[] { "contextualize", "partial" }, order);
    }

    [Fact]
    public void GetService_ResolvesFromRequestServices()
    {
        var service = new object();
        var tagHelper = new SampleViewTagHelper
        {
            ViewContext = CreateViewContext(s => s.AddSingleton(service))
        };

        Assert.Same(service, tagHelper.Resolve<object>());
    }

    [Fact]
    public void GetService_NotRegistered_Throws()
    {
        var tagHelper = new SampleViewTagHelper { ViewContext = CreateViewContext() };

        Assert.Throws<InvalidOperationException>(() => tagHelper.Resolve<ViewTagHelperTests>());
    }

    [Fact]
    public async Task ProcessAsync_CallsProcessView()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.True(tagHelper.ProcessViewCalled);
    }

    [Fact]
    public async Task ProcessAsync_NoViewName_UsesClassNameWithoutTagHelperSuffix()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Equal("SampleView", tagHelper.ViewName);
        Assert.Equal("TagHelpers/SampleView", Assert.Single(fixture.PartialCalls).ViewName);
    }

    [Fact]
    public async Task ProcessAsync_ViewNameSetInProcessView_IsUsed()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext, ViewNameToSet = "Custom" };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Equal("TagHelpers/Custom", Assert.Single(fixture.PartialCalls).ViewName);
    }

    [Fact]
    public async Task ProcessAsync_NestedViewName_UsesForwardSlashesOnly()
    {
        // Compiled library views are matched by exact virtual path, so a backslash
        // (what Path.Combine produces on Windows) means the view is never found.
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext, ViewNameToSet = "SignInButtons/Google" };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        var name = Assert.Single(fixture.PartialCalls).ViewName;
        Assert.Equal("TagHelpers/SignInButtons/Google", name);
        Assert.DoesNotContain('\\', name);
    }

    [Fact]
    public async Task ProcessAsync_PassesTagHelperAsViewModel()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput());

        Assert.Same(tagHelper, Assert.Single(fixture.PartialCalls).Model);
    }

    [Fact]
    public async Task ProcessAsync_SetsOutputContentToRenderedPartial()
    {
        var fixture = new HtmlHelperFixture("<p>hello</p>");
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };
        var output = CreateOutput();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.Equal("<p>hello</p>", output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_InnerHtmlEmpty_IsFilledFromChildContent()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput(childContent: "inner text"));

        Assert.Equal("inner text", tagHelper.InnerHtml);
    }

    [Fact]
    public async Task ProcessAsync_InnerHtmlAlreadySet_IsNotOverwritten()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new SampleViewTagHelper { ViewContext = fixture.ViewContext, InnerHtml = "preset" };

        await tagHelper.ProcessAsync(CreateContext(), CreateOutput(childContent: "other"));

        Assert.Equal("preset", tagHelper.InnerHtml);
    }
}