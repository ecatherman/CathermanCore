using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using static CathermanCore.Web.Tests.TagHelperTestSupport;

namespace CathermanCore.Web.Tests;

public class PagingItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class PagingTestContext(DbContextOptions<PagingTestContext> options) : DbContext(options)
{
    public DbSet<PagingItem> Items => Set<PagingItem>();
}

public class PagingTests
{
    private static PagingTestContext CreateDb(int itemCount)
    {
        var options = new DbContextOptionsBuilder<PagingTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PagingTestContext(options);
        for (var i = 1; i <= itemCount; i++)
            db.Items.Add(new PagingItem { Id = i, Name = $"Item {i}" });
        db.SaveChanges();
        return db;
    }

    private static int[] Ids(List<PagingItem> items) => items.Select(i => i.Id).ToArray();

    [Fact]
    public void Defaults()
    {
        var paging = new Paging();

        Assert.Equal(100, paging.PageSize);
        Assert.Equal(0, paging.TotalCount);
        Assert.Equal("", paging.PageUrl);
        Assert.Equal(1, paging.PageNumber);
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(1, 100, 1)]
    [InlineData(100, 100, 1)]
    [InlineData(101, 100, 2)]
    [InlineData(250, 100, 3)]
    [InlineData(25, 10, 3)]
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expected)
    {
        var paging = new Paging { TotalCount = totalCount, PageSize = pageSize };

        Assert.Equal(expected, paging.TotalPages);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    [InlineData(999, 3)]
    public void PageNumber_IsClampedToValidRange(int requested, int expected)
    {
        var paging = new Paging { TotalCount = 250, PageSize = 100, PageNumber = requested };

        Assert.Equal(expected, paging.PageNumber);
    }

    [Fact]
    public void PageNumber_NoItems_IsOne()
    {
        var paging = new Paging { TotalCount = 0, PageNumber = 5 };

        Assert.Equal(1, paging.PageNumber);
    }

    [Fact]
    public async Task GetPage_FirstPage()
    {
        using var db = CreateDb(25);
        var paging = new Paging { PageSize = 10, PageNumber = 1 };

        var page = await paging.GetPage(db.Items.OrderBy(i => i.Id));

        Assert.Equal(Enumerable.Range(1, 10), Ids(page));
        Assert.Equal(25, paging.TotalCount);
        Assert.Equal(3, paging.TotalPages);
    }

    [Fact]
    public async Task GetPage_MiddlePage()
    {
        using var db = CreateDb(25);
        var paging = new Paging { PageSize = 10, PageNumber = 2 };

        var page = await paging.GetPage(db.Items.OrderBy(i => i.Id));

        Assert.Equal(Enumerable.Range(11, 10), Ids(page));
    }

    [Fact]
    public async Task GetPage_LastPartialPage()
    {
        using var db = CreateDb(25);
        var paging = new Paging { PageSize = 10, PageNumber = 3 };

        var page = await paging.GetPage(db.Items.OrderBy(i => i.Id));

        Assert.Equal(Enumerable.Range(21, 5), Ids(page));
    }

    [Fact]
    public async Task GetPage_PageNumberBeyondLast_ReturnsLastPage()
    {
        using var db = CreateDb(25);
        var paging = new Paging { PageSize = 10, PageNumber = 99 };

        var page = await paging.GetPage(db.Items.OrderBy(i => i.Id));

        Assert.Equal(Enumerable.Range(21, 5), Ids(page));
        Assert.Equal(3, paging.PageNumber);
    }

    [Fact]
    public async Task GetPage_PageSizeLargerThanTotal_ReturnsEverything()
    {
        using var db = CreateDb(7);
        var paging = new Paging { PageSize = 100 };

        var page = await paging.GetPage(db.Items.OrderBy(i => i.Id));

        Assert.Equal(7, page.Count);
        Assert.Equal(1, paging.TotalPages);
    }

    [Fact]
    public async Task GetPage_EmptyQuery_ReturnsEmptyListAndZeroTotals()
    {
        using var db = CreateDb(0);
        var paging = new Paging { PageSize = 10, PageNumber = 4 };

        var page = await paging.GetPage(db.Items.OrderBy(i => i.Id));

        Assert.Empty(page);
        Assert.Equal(0, paging.TotalCount);
        Assert.Equal(0, paging.TotalPages);
        Assert.Equal(1, paging.PageNumber);
    }

    [Fact]
    public async Task GetPage_CountsTheFilteredQueryNotTheTable()
    {
        using var db = CreateDb(25);
        var paging = new Paging { PageSize = 10 };

        var page = await paging.GetPage(db.Items.Where(i => i.Id > 20).OrderBy(i => i.Id));

        Assert.Equal(Enumerable.Range(21, 5), Ids(page));
        Assert.Equal(5, paging.TotalCount);
        Assert.Equal(1, paging.TotalPages);
    }
}

public class PagingTagHelperTests
{
    [Fact]
    public async Task ProcessAsync_RendersWithoutWrapperElement()
    {
        var fixture = new HtmlHelperFixture();
        var output = CreateOutput("paging");

        await new PagingTagHelper(fixture.Object).ProcessAsync(CreateContext("paging"), output);

        Assert.Equal("", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_RendersPagingPartialWithItselfAsModel()
    {
        var fixture = new HtmlHelperFixture();
        var tagHelper = new PagingTagHelper(fixture.Object);

        await tagHelper.ProcessAsync(CreateContext("paging"), CreateOutput("paging"));

        var call = Assert.Single(fixture.PartialCalls);
        Assert.Equal("TagHelpers/Paging", call.ViewName);
        Assert.Same(tagHelper, call.Model);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotModifyPaging()
    {
        // The view builds its links from Model.Paging.PageUrl (with a {PageNumber} placeholder),
        // so the tag helper must hand the caller's Paging object through untouched.
        var fixture = new HtmlHelperFixture();
        var paging = new Paging { PageUrl = "/works?page={PageNumber}", PageSize = 25, TotalCount = 80 };
        var tagHelper = new PagingTagHelper(fixture.Object) { Paging = paging };

        await tagHelper.ProcessAsync(CreateContext("paging"), CreateOutput("paging"));

        Assert.Same(paging, tagHelper.Paging);
        Assert.Equal("/works?page={PageNumber}", paging.PageUrl);
        Assert.Equal(25, paging.PageSize);
        Assert.Equal(80, paging.TotalCount);
    }

    [Fact]
    public async Task ProcessAsync_PagingIsReachableFromTheViewModel()
    {
        var fixture = new HtmlHelperFixture();
        var paging = new Paging { TotalCount = 250 };
        var tagHelper = new PagingTagHelper(fixture.Object) { Paging = paging };

        await tagHelper.ProcessAsync(CreateContext("paging"), CreateOutput("paging"));

        var model = Assert.IsType<PagingTagHelper>(Assert.Single(fixture.PartialCalls).Model);
        Assert.Same(paging, model.Paging);
    }

    [Fact]
    public void Paging_DefaultsToAnEmptyPagingInstance()
    {
        var tagHelper = new PagingTagHelper(new HtmlHelperFixture().Object);

        Assert.NotNull(tagHelper.Paging);
        Assert.Equal(0, tagHelper.Paging.TotalPages);
        Assert.Equal(1, tagHelper.Paging.PageNumber);
        Assert.Equal("", tagHelper.Paging.PageUrl);
    }
}