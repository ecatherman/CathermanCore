using CathermanCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace CathermanCore.EntityFramework;

public class BaseContextHandler<TContext>(TContext context, IOptions<Logging> loggingOptions) where TContext : DbContext, IHasUserNameProperty
{
    private readonly TContext Context = context;
    private readonly Logging Logging = loggingOptions.Value;

    public void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
        {
            warnings.Throw(CoreEventId.FirstWithoutOrderByAndFilterWarning);
            warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
        });

#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
#endif

        if (!Enum.TryParse(Logging.EfLogLevel, out Serilog.Events.LogEventLevel seriLevel))
            seriLevel = Serilog.Events.LogEventLevel.Error;

        if (!Enum.TryParse(Logging.EfLogLevel, out LogLevel eventLevel))
            eventLevel = LogLevel.Error;

        var options = DbContextLoggerOptions.DefaultWithUtcTime;

        optionsBuilder.LogTo(message => Serilog.Log.Write(seriLevel, message), eventLevel, options);
    }

    public void ApplyBaseConfigurations(ModelBuilder builder)
    {
        builder.EntitiesOfType<IEntity>(b =>
        {
            b.Property<DateTime>(nameof(IEntity.Added))
             .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            var param = Expression.Parameter(b.Metadata.ClrType, "p");
            var body = Expression.Equal(
                Expression.Property(param, nameof(IEntity.IsLive)),
                Expression.Constant(true)
            );
            b.HasQueryFilter(Expression.Lambda(body, param));
            b.Property(nameof(IEntity.LastUpdateBy)).HasDefaultValue("");
        });

        builder.EntitiesOfType<Entity>(b => b.Property(nameof(Entity.SourceData)).HasDefaultValue("N/A"));
    }

    public void UpdateEntityInfo()
    {
        Context.ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;

        var softDeletedEntries = Context.ChangeTracker.Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Modified && !e.Entity.IsLive && e.Property(x => x.IsLive).IsModified)
            .ToList();

        foreach (var entry in softDeletedEntries)
        {
            foreach (var navigation in entry.Navigations)
            {
                if (navigation.CurrentValue is IEnumerable<object> children)
                {
                    foreach (var child in children.OfType<IConnectingEntity>().Where(c => c.IsLive))
                        child.IsLive = false;
                }
                else if (navigation.CurrentValue is IConnectingEntity singleChild && singleChild.IsLive)
                {
                    singleChild.IsLive = false;
                }
            }
        }

        foreach (var entry in Context.ChangeTracker.Entries<IEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Added = now;
                entry.Entity.Updated = now;
                entry.Entity.LastUpdateBy = Context.UserName;
                entry.Entity.IsLive = true;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.Updated = now;
                entry.Entity.LastUpdateBy = Context.UserName;
            }
        }
    }

}
