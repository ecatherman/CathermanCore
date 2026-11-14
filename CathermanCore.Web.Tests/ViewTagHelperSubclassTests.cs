using BibleWork.Pages.TagHelpers;
using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Reflection;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

public class ViewTagHelperSubclassTests
{
    public static TheoryData<string, Func<ViewTagHelper>> WrapperlessHelpers => new()
    {
        { "Modal", () => new ModalTagHelper() },
        { "DialogBox", () => new DialogBoxTagHelper() },
        { "IframeModal", () => new IframeModalTagHelper() },
        { "Paging", () => new PagingTagHelper() },
    };

    [Theory]
    [MemberData(nameof(WrapperlessHelpers))]
    public async Task ProcessAsync_RendersExpectedViewWithoutWrapperElement(
        string expectedView, Func<ViewTagHelper> create)
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = create();
        tagHelper.ViewContext = fixture.ViewContext;
        var output = CreateOutput("tag");

        await tagHelper.ProcessAsync(CreateContext("tag"), output);

        Assert.Equal("", output.TagName);
        var call = Assert.Single(fixture.PartialCalls);
        Assert.Equal($"TagHelpers/{expectedView}", call.ViewName);
        Assert.Same(tagHelper, call.Model);
    }

    [Fact]
    public void Modal_Defaults()
    {
        var modal = new ModalTagHelper();

        Assert.Equal("", modal.Header);
        Assert.Equal("", modal.Id);
        Assert.False(modal.Write);
        Assert.Equal("", modal.CustomCallback);
        Assert.Equal("Delete", modal.CustomText);
        Assert.Equal("", modal.Size);
    }

    [Fact]
    public void IframeModal_IdDefaultsToEmpty()
    {
        Assert.Equal("", new IframeModalTagHelper().Id);
    }

    [Fact]
    public async Task Modal_ChildContentIsAvailableToTheView()
    {
        var fixture = new HtmlHelperFixture();
        var modal = new ModalTagHelper { ViewContext = fixture.ViewContext, Id = "m1", Header = "Title" };

        await modal.ProcessAsync(CreateContext("modal"), CreateOutput("modal", "body content"));

        Assert.Equal("body content", modal.InnerHtml);
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("Microsoft")]
    public async Task ProviderButton_RendersProviderViewWithForwardSlashes(string provider)
    {
        // Fails on Windows if ProviderButtonTagHelper still builds the name with Path.Combine,
        // which yields "SignInButtons\Google" and a view path that is never found.
        var fixture = new HtmlHelperFixture();
        var tagHelper = new ProviderButtonTagHelper { ViewContext = fixture.ViewContext, Provider = provider };

        await tagHelper.ProcessAsync(CreateContext("button"), CreateOutput("button"));

        Assert.Equal($"TagHelpers/SignInButtons/{provider}", Assert.Single(fixture.PartialCalls).ViewName);
    }
}

/// <summary>
/// The Razor markup binds to these tag/attribute names, and a mismatch fails silently
/// (the element is simply left as plain HTML), so pin them down.
/// </summary>
public class TagHelperTargetingTests
{
    private static HtmlTargetElementAttribute Target(Type type) =>
        type.GetCustomAttributes<HtmlTargetElementAttribute>().Single();

    [Theory]
    [InlineData(typeof(IfTagHelper), "*", "if")]
    [InlineData(typeof(HideTagHelper), "*", "hide")]
    [InlineData(typeof(ShowTagHelper), "*", "show")]
    [InlineData(typeof(ProviderButtonTagHelper), "button", "provider")]
    public void RequiresAttribute(Type tagHelper, string tag, string attribute)
    {
        var target = Target(tagHelper);

        Assert.Equal(tag, target.Tag);
        Assert.Equal(attribute, target.Attributes);
    }

    [Fact]
    public void FancyCheckbox_TargetsFancyCheckboxElementIncludingSelfClosing()
    {
        var target = Target(typeof(FancyCheckboxTagHelper));

        Assert.Equal("fancy-checkbox", target.Tag);
        Assert.Equal(TagStructure.NormalOrSelfClosing, target.TagStructure);
    }
}