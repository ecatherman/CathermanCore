using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CathermanCore.Web.TagHelpers;

public abstract class ViewTagHelper : TagHelper
{
    protected T GetService<T>() where T : notnull =>
        ViewContext.HttpContext.RequestServices.GetRequiredService<T>();

    [HtmlAttributeNotBound]
    public virtual string ViewName { get; set; } = "";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeNotBound]
    public string InnerHtml { get; set; } = "";

    public async sealed override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var htmlHelper = GetService<IHtmlHelper>();
        var viewContextAware = htmlHelper as IViewContextAware ??
            throw new Exception($"Error converting {nameof(IHtmlHelper)} to {nameof(IViewContextAware)}");
        viewContextAware.Contextualize(ViewContext);

        ProcessView(context, output);

        if (!InnerHtml.HasValue())
            InnerHtml = (await output.GetChildContentAsync()).GetContent();

        if (!ViewName.HasValue())
            ViewName = GetType().Name.Replace(nameof(TagHelper), "", StringComparison.InvariantCultureIgnoreCase);

        output.Content.SetHtmlContent(await htmlHelper.PartialAsync($"TagHelpers/{ViewName}", this));
    }

    protected abstract void ProcessView(TagHelperContext context, TagHelperOutput output);

}

