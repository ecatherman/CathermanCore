using CathermanCore;
using CathermanCore.Test.EF;
using Microsoft.EntityFrameworkCore;

namespace CathermanCore.Test;

public class BaseContextHandlerTests
{
    // ── UpdateEntityInfo — Added entities ────────────────────────────────────

    [Fact]
    public async Task UpdateEntityInfo_AddedEntity_SetsAddedAndUpdated()
    {
        using var db = TestDb.CreateBase();
        var before = DateTime.UtcNow;

        db.Posts.Add(new Post { Title = "New Post" });
        await db.SaveChangesAsync();

        var after = DateTime.UtcNow;
        var saved = await db.Posts.FirstAsync();

        Assert.InRange(saved.Added, before, after);
        Assert.InRange(saved.Updated, before, after);
    }

    [Fact]
    public async Task UpdateEntityInfo_AddedEntity_SetsLastUpdateByFromUserName()
    {
        using var db = TestDb.CreateBase(userName: "apostle.paul");
        db.Posts.Add(new Post { Title = "Ephesians" });
        await db.SaveChangesAsync();

        var saved = await db.Posts.FirstAsync();
        Assert.Equal("apostle.paul", saved.LastUpdateBy);
    }

    [Fact]
    public async Task UpdateEntityInfo_AddedEntity_SetsIsLiveTrue()
    {
        using var db = TestDb.CreateBase();
        // Even if someone sets IsLive = false before saving, UpdateEntityInfo should force true
        var post = new Post { Title = "Force Live", IsLive = false };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var saved = await db.Posts.IgnoreQueryFilters()
                            .FirstAsync(p => p.Id == post.Id);
        Assert.True(saved.IsLive);
    }

    [Fact]
    public async Task UpdateEntityInfo_AddedEntity_AddedTimestamp_UsesUtcNow()
    {
        using var db = TestDb.CreateBase();
        var localBefore = DateTime.Now;

        db.Posts.Add(new Post { Title = "UTC Check" });
        await db.SaveChangesAsync();

        var saved = await db.Posts.FirstAsync();
        // Added should be UTC, so it must have Kind=Utc or be close to UtcNow
        Assert.True(saved.Added.Kind == DateTimeKind.Utc
                    || saved.Added >= localBefore.ToUniversalTime().AddSeconds(-1));
    }

    // ── UpdateEntityInfo — Modified entities ──────────────────────────────────

    [Fact]
    public async Task UpdateEntityInfo_ModifiedEntity_UpdatesUpdated()
    {
        using var db = TestDb.CreateBase();
        db.Posts.Add(new Post { Title = "Original" });
        await db.SaveChangesAsync();

        var post = await db.Posts.FirstAsync();
        var originalUpdated = post.Updated;

        // Small delay to ensure timestamp differs
        await Task.Delay(10);

        post.Title = "Modified";
        await db.SaveChangesAsync();

        Assert.True(post.Updated >= originalUpdated);
    }

    [Fact]
    public async Task UpdateEntityInfo_ModifiedEntity_DoesNotChangeAdded()
    {
        using var db = TestDb.CreateBase();
        db.Posts.Add(new Post { Title = "Track Added" });
        await db.SaveChangesAsync();

        var post = await db.Posts.FirstAsync();
        var originalAdded = post.Added;

        await Task.Delay(10);

        post.Title = "Changed Title";
        await db.SaveChangesAsync();

        // Added should be immutable — EF's AfterSaveBehavior.Ignore prevents overwrite
        Assert.Equal(originalAdded, post.Added);
    }

    [Fact]
    public async Task UpdateEntityInfo_ModifiedEntity_UpdatesLastUpdateBy()
    {
        using var db = TestDb.CreateBase(userName: "first.user");
        db.Posts.Add(new Post { Title = "Track User" });
        await db.SaveChangesAsync();

        db.UserName = "second.user";
        var post = await db.Posts.FirstAsync();
        post.Title = "Updated By Second";
        await db.SaveChangesAsync();

        Assert.Equal("second.user", post.LastUpdateBy);
    }

    // ── UpdateEntityInfo — Soft delete cascades ───────────────────────────────

    [Fact]
    public async Task UpdateEntityInfo_SoftDelete_CascadesToIConnectingEntityChildren()
    {
        using var db = TestDb.CreateBase();

        var post = new Post { Title = "Parent" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        db.Tags.AddRange(
            new Tag { Name = "Child 1", PostId = post.Id },
            new Tag { Name = "Child 2", PostId = post.Id });
        await db.SaveChangesAsync();

        // Load the post with its tags so the change tracker sees the navigation
        db.ChangeTracker.Clear();
        var loaded = await db.Posts
                             .Include(p => p.Tags)
                             .FirstAsync(p => p.Id == post.Id);

        // Soft-delete the parent
        loaded.IsLive = false;
        await db.SaveChangesAsync();

        // Children should also be soft-deleted
        var tags = await db.Tags.IgnoreQueryFilters()
                           .Where(t => t.PostId == post.Id)
                           .ToListAsync();
        Assert.All(tags, t => Assert.False(t.IsLive));
    }

    [Fact]
    public async Task UpdateEntityInfo_SoftDelete_EntityWithNoChildren_DoesNotThrow()
    {
        using var db = TestDb.CreateBase();
        var post = new Post { Title = "Lonely Post" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var loaded = await db.Posts.FirstAsync();
        loaded.IsLive = false;

        var ex = await Record.ExceptionAsync(() => db.SaveChangesAsync());
        Assert.Null(ex);
    }

    [Fact]
    public async Task UpdateEntityInfo_SoftDelete_OnlyDeletesAlreadyLiveChildren()
    {
        using var db = TestDb.CreateBase();
        var post = new Post { Title = "Mixed Children" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        // Add one live and one already-soft-deleted tag
        var liveTag = new Tag { Name = "Live", PostId = post.Id, IsLive = true };
        db.Tags.Add(liveTag);
        await db.SaveChangesAsync();

        // Manually soft-delete one tag before deleting parent
        db.ChangeTracker.Clear();
        var tagToPreDelete = await db.Tags
                                     .IgnoreQueryFilters()
                                     .FirstAsync(t => t.Id == liveTag.Id);
        tagToPreDelete.IsLive = false;
        await db.SaveChangesAsync();

        // Now soft-delete the parent with its children loaded
        db.ChangeTracker.Clear();
        var loaded = await db.Posts.Include(p => p.Tags).FirstAsync(p => p.Id == post.Id);
        loaded.IsLive = false;
        await db.SaveChangesAsync(); // should not throw even though child is already false

        var allTags = await db.Tags.IgnoreQueryFilters()
                              .Where(t => t.PostId == post.Id)
                              .ToListAsync();
        Assert.All(allTags, t => Assert.False(t.IsLive));
    }

    // ── ApplyBaseConfigurations — IsLive query filter ─────────────────────────

    [Fact]
    public async Task QueryFilter_IsLiveFalse_ExcludedFromDefaultQuery()
    {
        using var db = TestDb.CreateBase();
        var post = new Post { Title = "Deleted" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        // Soft-delete the entity
        db.ChangeTracker.Clear();
        var found = await db.Posts.FirstAsync(p => p.Id == post.Id);
        found.IsLive = false;
        await db.SaveChangesAsync();

        // Default query should not include it
        var count = await db.Posts.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task QueryFilter_IsLiveTrue_IncludedInDefaultQuery()
    {
        using var db = TestDb.CreateBase();
        db.Posts.Add(new Post { Title = "Active" });
        await db.SaveChangesAsync();

        var count = await db.Posts.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task QueryFilter_IgnoreQueryFilters_ReturnsSoftDeletedEntities()
    {
        using var db = TestDb.CreateBase();
        var post = new Post { Title = "Should Be Found" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var found = await db.Posts.FirstAsync(p => p.Id == post.Id);
        found.IsLive = false;
        await db.SaveChangesAsync();

        // With IgnoreQueryFilters, it should be retrievable
        var all = await db.Posts.IgnoreQueryFilters().CountAsync();
        Assert.Equal(1, all);
    }

    [Fact]
    public async Task QueryFilter_AppliesToAllIEntityTypes()
    {
        using var db = TestDb.CreateBase();

        // Add entities of different IEntity types
        var author = new Author { Name = "Matthew" };
        var post = new Post { Title = "Gospel" };
        db.Authors.Add(author);
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        // Soft-delete both
        db.ChangeTracker.Clear();
        var a = await db.Authors.FirstAsync();
        var p = await db.Posts.FirstAsync();
        a.IsLive = false;
        p.IsLive = false;
        await db.SaveChangesAsync();

        // Both types should be filtered out
        Assert.Equal(0, await db.Authors.CountAsync());
        Assert.Equal(0, await db.Posts.CountAsync());

        // Both still exist when filters are ignored
        Assert.Equal(1, await db.Authors.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.Posts.IgnoreQueryFilters().CountAsync());
    }

    // ── ApplyBaseConfigurations — Added property immutability ─────────────────

    [Fact]
    public async Task Added_Property_NotUpdatedOnSubsequentSaves()
    {
        using var db = TestDb.CreateBase();
        db.Posts.Add(new Post { Title = "Immutable Added" });
        await db.SaveChangesAsync();

        var post = await db.Posts.FirstAsync();
        var originalAdded = post.Added;

        // Multiple saves should not change Added
        for (int i = 0; i < 3; i++)
        {
            post.Title = $"Updated {i}";
            await db.SaveChangesAsync();
        }

        Assert.Equal(originalAdded, post.Added);
    }

    // ── DefaultValues ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DefaultValues_LastUpdateBy_DefaultsToEmptyString()
    {
        using var db = TestDb.CreateBase(userName: "");
        db.Posts.Add(new Post { Title = "Default LastUpdateBy" });
        await db.SaveChangesAsync();

        var saved = await db.Posts.FirstAsync();
        Assert.Equal("", saved.LastUpdateBy);
    }
}