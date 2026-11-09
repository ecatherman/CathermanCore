using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CathermanCore.Web.TagHelpers;

public abstract class ViewTagHelper : TagHelper
{
    private readonly IHtmlHelper HtmlHelper = GetService<IHtmlHelper>();
    private readonly IViewContextAware ViewContextAware = HtmlHelper as IViewContextAware ?? 
        throw new Exception($"Error converting {nameof(IHtmlHelper)} to {nameof(IViewContextAware)}");

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
        ViewContextAware.Contextualize(ViewContext);

        ProcessView(context, output);

        if (!InnerHtml.HasValue())
            InnerHtml = (await output.GetChildContentAsync()).GetContent();

        if (!ViewName.HasValue())
            ViewName = GetType().Name.Replace(nameof(TagHelper), "");

        output.Content.SetHtmlContent(await HtmlHelper.PartialAsync($"TagHelpers/{ViewName}", this));
    }

    protected abstract void ProcessView(TagHelperContext context, TagHelperOutput output);

}

