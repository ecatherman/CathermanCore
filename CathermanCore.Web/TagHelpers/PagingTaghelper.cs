using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace CathermanCore.Web.TagHelpers;

public class PagingTagHelper(IHtmlHelper htmlHelper) : ViewTagHelper(htmlHelper)
{
    public Paging Paging { get; set; } = new();
    protected override void ProcessView(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "";
    }
}

public class Paging
{
    public int TotalCount { get; set; } = 0;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string PageUrl { get; set; } = "";
    public int PageSize { get; set; } = 100;
    private int _pageNumber = 1;
    public int PageNumber 
    { 
        get => _pageNumber > 0 && TotalPages > 0 ? Math.Min(_pageNumber, TotalPages) : 1; 
        set => _pageNumber = value; 
    }

    public async Task<List<T>> GetPage<T>(IQueryable<T> query)
    {
        TotalCount = await query.CountAsync();
        return await query.Skip(Math.Max(0, PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
    }
}

