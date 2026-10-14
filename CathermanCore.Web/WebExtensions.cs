using Microsoft.Extensions.DependencyInjection;

namespace CathermanCore.Web;

public static class WebExtensions
{
    public static IMvcBuilder AddCathermanWebViews(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(WebExtensions).Assembly);
}