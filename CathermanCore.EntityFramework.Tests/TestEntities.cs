using CathermanCore;
using CathermanCore.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CathermanCore.Test.EF;

// ── Test entities ────────────────────────────────────────────────────────────

public class Author : Entity
{
    public string Name { get; set; } = "";
    public List<Post> Posts { get; set; } = [];
}

/// <summary>ISoftDelete entity — soft-delete should cascade to its Tags.</summary>
public class Post : Entity, ISoftDelete
{
    public string Title { get; set; } = "";
    public int AuthorId { get; set; }
    public Author? Author { get; set; }
    public List<Tag> Tags { get; set; } = [];
}

/// <summary>IConnectingEntity — should be soft-deleted when its parent Post is deleted.</summary>
public class Tag : Entity, IConnectingEntity
{
    public string Name { get; set; } = "";
    public int PostId { get; set; }
    public Post? Post { get; set; }
}

// ── Minimal DbContext (no BaseContext) for EntityFrameworkExtensions tests ───

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
}

// ── Context that wires BaseContextHandler for UpdateEntityInfo / Config tests ──

public class TestBaseContext : DbContext, IHasUserNameProperty
{
    private readonly BaseContextHandler<TestBaseContext> _handler;

    public TestBaseContext(DbContextOptions<TestBaseContext> options, IOptions<Logging> logging)
        : base(options)
    {
        _handler = new BaseContextHandler<TestBaseContext>(this, logging);
    }

    public string UserName { get; set; } = "tester";

    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        _handler.ApplyBaseConfigurations(builder);

        builder.Entity<Post>()
               .HasMany(p => p.Tags)
               .WithOne(t => t.Post)
               .HasForeignKey(t => t.PostId);

        builder.Entity<Author>()
               .HasMany(a => a.Posts)
               .WithOne(p => p.Author)
               .HasForeignKey(p => p.AuthorId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        _handler.UpdateEntityInfo();
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        _handler.UpdateEntityInfo();
        return base.SaveChanges();
    }
}

// ── Factory helpers ───────────────────────────────────────────────────────────

public static class TestDb
{
    /// <summary>Creates a fresh in-memory TestDbContext with a unique DB name.</summary>
    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    /// <summary>Creates a fresh in-memory TestBaseContext (applies query filters + handler).</summary>
    public static TestBaseContext CreateBase(string? userName = null)
    {
        var options = new DbContextOptionsBuilder<TestBaseContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var logging = Options.Create(new Logging { EfLogLevel = "Error" });
        var ctx = new TestBaseContext(options, logging);
        if (userName != null) ctx.UserName = userName;
        return ctx;
    }
}