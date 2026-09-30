using CathermanCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace CathermanCore.EntityFramework;

public abstract class BaseContext : DbContext, IHasUserNameProperty
{
    protected readonly BaseContextHandler<BaseContext> Handler;

    protected BaseContext(DbContextOptions<BaseContext> options, IOptions<Logging> loggingOptions) : base(options)
    {
        Handler = new BaseContextHandler<BaseContext>(this, loggingOptions);
    }

    public string UserName { get; set; } = "";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        Handler.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        Handler.ApplyBaseConfigurations(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Handler.UpdateEntityInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        Handler.UpdateEntityInfo();
        return base.SaveChanges();
    }
}