using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CathermanCore.Web.TagHelpers;

public abstract class ViewTagHelper(IHtmlHelper htmlHelper) : TagHelper
{
    private readonly IHtmlHelper HtmlHelper = htmlHelper;
    private readonly IViewContextAware ViewContextAware = htmlHelper as IViewContextAware ?? 
        throw new Exception($"Error converting {nameof(IHtmlHelper)} to {nameof(IViewContextAware)}");

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

