using System.Reflection;
using CathermanCore.Web.TagHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CathermanCore.Web.Tests;

/// <summary>
/// Guards the wiring that makes the library's compiled views reachable from a host app.
/// A missing application part produces "The partial view 'TagHelpers/Paging' was not found"
/// at runtime even though the views are compiled into the library.
///
/// These build a real (but never started) WebApplication, so they are the most
/// environment-dependent tests in this project. If one fails during setup rather than
/// on its assertion, the harness needs adjusting, not the library.
/// </summary>
public class ViewDiscoveryTests
{
    private static readonly Assembly LibraryAssembly = typeof(ViewTagHelper).Assembly;

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRazorPages().AddCathermanWebViews();
        return builder.Build();
    }

    [Fact]
    public void AddCathermanWebViews_RegistersLibraryAsApplicationPart()
    {
        using var app = BuildApp();

        var manager = app.Services.GetRequiredService<ApplicationPartManager>();

        Assert.Contains(manager.ApplicationParts, p => p.Name == LibraryAssembly.GetName().Name);
    }

    [Fact]
    public void EveryViewTagHelper_HasACompiledView()
    {
        var compiledViews = LibraryAssembly.GetCustomAttributes<RazorCompiledItemAttribute>()
            .Where(a => a.Kind == "mvc.1.0.view")
            .Select(a => a.Identifier)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ProviderButtonTagHelper picks its view per provider (SignInButtons/<Provider>),
        // so it is covered by the view-engine test below instead.
        var missing = LibraryAssembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && typeof(ViewTagHelper).IsAssignableFrom(t)
                        && t != typeof(ProviderButtonTagHelper))
            .Select(t => $"/Views/Shared/TagHelpers/{t.Name.Replace("TagHelper", "")}.cshtml")
            .Where(path => !compiledViews.Contains(path))
            .ToList();

        Assert.Empty(missing);
    }

    [Theory]
    [InlineData("Paging")]
    [InlineData("Modal")]
    [InlineData("DialogBox")]
    [InlineData("IframeModal")]
    [InlineData("AccordionItem")]
    [InlineData("SignInButtons/Google")]
    public void RazorViewEngine_FindsLibraryView(string viewName)
    {
        using var app = BuildApp();
        var engine = app.Services.GetRequiredService<IRazorViewEngine>();
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = app.Services },
            new RouteData(),
            new ActionDescriptor { RouteValues = new Dictionary<string, string?>() });

        // Same call ViewTagHelper makes: a partial view named "TagHelpers/<ViewName>".
        var result = engine.FindView(actionContext, $"TagHelpers/{viewName}", isMainPage: false);

        Assert.True(result.Success,
            "Searched: " + string.Join(", ", result.SearchedLocations ?? Enumerable.Empty<string>()));
    }
}