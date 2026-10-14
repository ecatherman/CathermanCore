using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

public class IfTagHelperTests
{
    [Fact]
    public void If_DefaultsToTrue() => Assert.True(new IfTagHelper().If);

    [Fact]
    public void Process_IfTrue_KeepsOutput()
    {
        var output = CreateOutput("div");

        new IfTagHelper { If = true }.Process(CreateContext(), output);

        Assert.Equal("div", output.TagName);
    }

    [Fact]
    public void Process_IfFalse_SuppressesOutput()
    {
        var output = CreateOutput("div", attributes: new TagHelperAttributeList { { "class", "x" } });

        new IfTagHelper { If = false }.Process(CreateContext(), output);

        Assert.Null(output.TagName);
        Assert.True(output.Content.IsEmptyOrWhiteSpace);
    }
}

public class HideShowTagHelperTests
{
    private static string? Style(TagHelperOutput output) => output.Attributes["style"]?.Value?.ToString();

    [Fact]
    public void Hide_DefaultsToTrue() => Assert.True(new HideTagHelper().Hide);

    [Fact]
    public void Show_DefaultsToTrue() => Assert.True(new ShowTagHelper().Show);

    [Fact]
    public void Hide_True_AddsDisplayNone()
    {
        var output = CreateOutput();

        new HideTagHelper { Hide = true }.Process(CreateContext(), output);

        Assert.Equal("display: none;", Style(output));
    }

    [Fact]
    public void Hide_False_LeavesOutputUntouched()
    {
        var output = CreateOutput();

        new HideTagHelper { Hide = false }.Process(CreateContext(), output);

        Assert.Empty(output.Attributes);
    }

    [Fact]
    public void Hide_True_MergesWithExistingStyle()
    {
        var output = CreateOutput(attributes: new TagHelperAttributeList { { "style", "color: red" } });

        new HideTagHelper { Hide = true }.Process(CreateContext(), output);

        Assert.Equal("color: red;display: none;", Style(output));
        Assert.Single(output.Attributes, a => a.Name == "style");
    }

    [Fact]
    public void Show_True_LeavesOutputUntouched()
    {
        var output = CreateOutput();

        new ShowTagHelper { Show = true }.Process(CreateContext(), output);

        Assert.Empty(output.Attributes);
    }

    [Fact]
    public void Show_False_AddsDisplayNone()
    {
        var output = CreateOutput();

        new ShowTagHelper { Show = false }.Process(CreateContext(), output);

        Assert.Equal("display: none;", Style(output));
    }
}

public class AddOrAppendTests
{
    [Fact]
    public void NoExistingAttribute_AddsValueWithTrailingSemicolon()
    {
        var attributes = new TagHelperAttributeList();

        attributes.AddOrAppend("style", "display:none");

        Assert.Equal("display:none;", attributes["style"].Value?.ToString());
    }

    [Fact]
    public void ValueAlreadyEndsWithSemicolon_IsNotDoubled()
    {
        var attributes = new TagHelperAttributeList();

        attributes.AddOrAppend("style", "display:none;");

        Assert.Equal("display:none;", attributes["style"].Value?.ToString());
    }

    [Theory]
    [InlineData("color: red", "color: red;display:none;")]
    [InlineData("color: red;", "color: red;display:none;")]
    public void ExistingStyle_IsAppendedAfterNewValue(string existing, string expected)
    {
        var attributes = new TagHelperAttributeList { { "style", existing } };

        attributes.AddOrAppend("style", "display:none");

        Assert.Equal(expected, attributes["style"].Value?.ToString());
    }

    [Fact]
    public void ExistingStyle_IsReplacedNotDuplicated()
    {
        var attributes = new TagHelperAttributeList { { "style", "color: red" } };

        attributes.AddOrAppend("style", "display:none");

        Assert.Single(attributes, a => a.Name == "style");
    }

    [Fact]
    public void NonStyleKey_LeavesExistingStyleAttributeUntouched()
    {
        // EXPECTED TO FAIL against the current implementation: AddOrAppend always looks up
        // "style" (a.Name == "style") regardless of the key it is given, so calling it with a
        // different key removes the style attribute and folds its value into the new one.
        // Fix: look the attribute up by key (a.Name == key).
        var attributes = new TagHelperAttributeList { { "style", "color: red;" } };

        attributes.AddOrAppend("data-x", "1");

        Assert.Equal("color: red;", attributes["style"]?.Value?.ToString());
        Assert.NotNull(attributes["data-x"]);
    }
}