using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CathermanCore.Web.Tests;

internal static class TagHelperTestSupport
{
    public static TagHelperContext CreateContext(
        string tagName = "div",
        TagHelperAttributeList? attributes = null,
        string uniqueId = "unique-id") =>
        new(tagName, attributes ?? new TagHelperAttributeList(), new Dictionary<object, object>(), uniqueId);

    public static TagHelperOutput CreateOutput(
        string tagName = "div",
        string childContent = "",
        TagHelperAttributeList? attributes = null) =>
        new(tagName, attributes ?? new TagHelperAttributeList(), (useCachedResult, encoder) =>
        {
            var content = new DefaultTagHelperContent();
            content.SetContent(childContent);
            return Task.FromResult<TagHelperContent>(content);
        });

    /// <summary>
    /// A ViewContext whose HttpContext.RequestServices is a real container holding whatever
    /// <paramref name="configure"/> registers. ViewTagHelper resolves IHtmlHelper from there.
    /// </summary>
    public static ViewContext CreateViewContext(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return new ViewContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
        };
    }
}

/// <summary>
/// A mocked IHtmlHelper that also implements IViewContextAware (ViewTagHelper requires that)
/// and records every PartialAsync call. <see cref="ViewContext"/> is set up to resolve
/// that helper from RequestServices, so assign it to a tag helper's ViewContext property.
/// </summary>
internal sealed class HtmlHelperFixture
{
    public Mock<IHtmlHelper> Mock { get; } = new();
    public Mock<IViewContextAware> ContextAware { get; }
    public List<(string ViewName, object? Model)> PartialCalls { get; } = new();
    public IHtmlHelper Object => Mock.Object;
    public ViewContext ViewContext { get; }

    public HtmlHelperFixture(string partialHtml = "<partial />")
    {
        ContextAware = Mock.As<IViewContextAware>();

        // The two-argument PartialAsync(name, model) used by ViewTagHelper is an extension method
        // that forwards to this three-argument interface method with a null ViewDataDictionary.
        Mock.Setup(h => h.PartialAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
            .Callback<string, object?, ViewDataDictionary?>((name, model, _) => PartialCalls.Add((name, model)))
            .ReturnsAsync((IHtmlContent)new HtmlString(partialHtml));

        // Must come after Mock.As<>, which can't be called once Mock.Object has been created.
        ViewContext = TagHelperTestSupport.CreateViewContext(s => s.AddSingleton(Mock.Object));
    }
}