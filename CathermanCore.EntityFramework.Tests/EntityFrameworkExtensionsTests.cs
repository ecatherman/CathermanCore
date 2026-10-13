using CathermanCore;
using CathermanCore.EntityFramework;
using CathermanCore.Test.EF;
using Microsoft.EntityFrameworkCore;

namespace CathermanCore.Test;

public class EntityFrameworkExtensionsTests
{
    // ── AddIfMissing ─────────────────────────────────────────────────────────

    [Fact]
    public void AddIfMissing_NullEntity_DoesNotThrow()
    {
        using var db = TestDb.Create();
        db.AddIfMissing<Post>(null!); // should silently return
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void AddIfMissing_NewEntity_IdZero_SetsStateAdded()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "New" };

        db.AddIfMissing(post);

        Assert.Equal(EntityState.Added, db.Entry(post).State);
    }

    [Fact]
    public void AddIfMissing_ExistingEntity_IdPositive_SetsStateUnchanged()
    {
        using var db = TestDb.Create();
        // Simulate a pre-existing entity with a known Id
        var post = new Post { Id = 1, Title = "Existing" };

        db.AddIfMissing(post);

        // Entity with Id > 0 is attached, not added
        Assert.Equal(EntityState.Unchanged, db.Entry(post).State);
    }

    [Fact]
    public void AddIfMissing_AlreadyTracked_DoesNotDuplicate()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Tracked" };
        db.Posts.Add(post);

        db.AddIfMissing(post); // called again — should no-op

        Assert.Single(db.ChangeTracker.Entries<Post>());
    }

    // ── FindAllAsync<T> ───────────────────────────────────────────────────────

    [Fact]
    public async Task FindAllAsync_NullContext_ThrowsArgumentNullException()
    {
        DbContext? ctx = null;
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => ctx!.FindAllAsync<Post>([1]));
    }

    [Fact]
    public async Task FindAllAsync_NullKeys_ThrowsArgumentNullException()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => db.FindAllAsync<Post>(null!));
    }

    [Fact]
    public async Task FindAllAsync_EmptyKeys_ReturnsEmpty()
    {
        using var db = TestDb.Create();
        var result = await db.FindAllAsync<Post>([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindAllAsync_ValidIds_ReturnsMatchingEntities()
    {
        using var db = TestDb.Create();
        db.Posts.AddRange(
            new Post { Title = "A" },
            new Post { Title = "B" },
            new Post { Title = "C" });
        await db.SaveChangesAsync();

        var all = await db.Posts.ToListAsync();
        var ids = all.Take(2).Select(p => p.Id).ToList();

        var result = (await db.FindAllAsync<Post>(ids)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Contains(p.Id, ids));
    }

    [Fact]
    public async Task FindAllAsync_SomeIdsNotFound_ReturnsOnlyFound()
    {
        using var db = TestDb.Create();
        db.Posts.Add(new Post { Title = "Only" });
        await db.SaveChangesAsync();
        var realId = db.Posts.First().Id;

        var result = (await db.FindAllAsync<Post>([realId, 99999])).ToList();

        Assert.Single(result);
        Assert.Equal(realId, result[0].Id);
    }

    [Fact]
    public async Task FindAllAsync_AllIdsNotFound_ReturnsEmpty()
    {
        using var db = TestDb.Create();
        var result = await db.FindAllAsync<Post>([99998, 99999]);
        Assert.Empty(result);
    }

    // ── FindAllAsync(Type, ...) ───────────────────────────────────────────────

    [Fact]
    public async Task FindAllAsync_TypeNotIEntity_ThrowsNotSupportedException()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => db.FindAllAsync(typeof(string), [1]));
    }

    [Fact]
    public async Task FindAllAsync_NullType_ThrowsArgumentNullException()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => db.FindAllAsync(null!, [1]));
    }

    [Fact]
    public async Task FindAllAsync_ValidType_ReturnsMatchingEntities()
    {
        using var db = TestDb.Create();
        db.Posts.AddRange(new Post { Title = "X" }, new Post { Title = "Y" });
        await db.SaveChangesAsync();

        var all = db.Posts.ToList();
        var ids = all.Select(p => p.Id).ToList();

        var result = (await db.FindAllAsync(typeof(Post), ids)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.IsType<Post>(e));
    }

    // ── DeleteAsync<T>(int) ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntity_SetsIsLiveFalse_ReturnsTrue()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Delete Me", IsLive = true };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var result = await db.DeleteAsync<Post>(post.Id);

        Assert.True(result);
        // Use IgnoreQueryFilters to verify it's still there but soft-deleted
        var deleted = await db.Posts.IgnoreQueryFilters()
                              .FirstAsync(p => p.Id == post.Id);
        Assert.False(deleted.IsLive);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentEntity_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var result = await db.DeleteAsync<Post>(99999);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithConditionTrue_DeletesAndReturnsTrue()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Conditional Delete", IsLive = true };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var result = await db.DeleteAsync<Post>(post.Id, p => p.Title == "Conditional Delete");

        Assert.True(result);
        var deleted = await db.Posts.IgnoreQueryFilters()
                              .FirstAsync(p => p.Id == post.Id);
        Assert.False(deleted.IsLive);
    }

    [Fact]
    public async Task DeleteAsync_WithConditionFalse_DoesNotDelete_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Keep Me", IsLive = true };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var result = await db.DeleteAsync<Post>(post.Id, p => p.Title == "Wrong Title");

        Assert.False(result);
        // Entity should still be live
        var stillLive = await db.Posts.FirstAsync(p => p.Id == post.Id);
        Assert.True(stillLive.IsLive);
    }

    // ── DeleteAsync<T>(IEnumerable<int>) ─────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_MultipleIds_AllSoftDeleted_ReturnsTrue()
    {
        using var db = TestDb.CreateBase();  // ← BaseContext has the IsLive filter
        db.Posts.AddRange(
            new Post { Title = "A", IsLive = true },
            new Post { Title = "B", IsLive = true });
        await db.SaveChangesAsync();

        var ids = db.Posts.Select(p => p.Id).ToList();
        var result = await db.DeleteAsync<Post>(ids);

        Assert.True(result);
        db.ChangeTracker.Clear();

        var remaining = await db.Posts.CountAsync();
        Assert.Equal(0, remaining);

        var allIgnoringFilters = await db.Posts.IgnoreQueryFilters().CountAsync();
        Assert.Equal(2, allIgnoringFilters);
    }

    [Fact]
    public async Task DeleteAsync_EmptyIds_ReturnsFalse()
    {
        using var db = TestDb.Create();
        var result = await db.DeleteAsync<Post>(Enumerable.Empty<int>());
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithCondition_OnlySomeDeleted()
    {
        using var db = TestDb.CreateBase();  // ← BaseContext has the IsLive filter
        db.Posts.AddRange(
            new Post { Title = "Delete", IsLive = true },
            new Post { Title = "Keep", IsLive = true });
        await db.SaveChangesAsync();

        var ids = db.Posts.Select(p => p.Id).ToList();
        await db.DeleteAsync<Post>(ids, p => p.Title == "Delete");

        db.ChangeTracker.Clear();

        var remainingLive = await db.Posts.CountAsync();
        Assert.Equal(1, remainingLive);
        Assert.Equal("Keep", (await db.Posts.FirstAsync()).Title);
    }

    // ── Detach ────────────────────────────────────────────────────────────────

    [Fact]
    public void Detach_TrackedEntity_SetsStateToDetached()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Tracked" };
        db.Posts.Add(post);

        Assert.Equal(EntityState.Added, db.Entry(post).State);

        db.Detach(post);

        Assert.Equal(EntityState.Detached, db.Entry(post).State);
    }

    [Fact]
    public void Detach_AlreadyDetachedEntity_DoesNotThrow()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Untracked" };
        // Not added to context — already detached
        var ex = Record.Exception(() => db.Detach(post));
        Assert.Null(ex);
    }

    // ── LoadAsync (guard conditions — no DB round-trip needed) ────────────────

    [Fact]
    public async Task LoadAsync_Reference_NullEntity_ThrowsArgumentNullException()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => db.LoadAsync<Post, Author>((Post)null!, p => p.Author!));
    }

    [Fact]
    public async Task LoadAsync_Reference_EntityIdZero_DoesNotThrow()
    {
        using var db = TestDb.Create();
        var post = new Post { Id = 0 }; // unsaved
        db.Posts.Attach(post);

        // Should return early without querying DB
        var ex = await Record.ExceptionAsync(
            () => db.LoadAsync<Post, Author>(post, p => p.Author!));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LoadAsync_Collection_NullEntity_ThrowsArgumentNullException()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => db.LoadAsync<Post, Tag>((Post)null!, p => p.Tags));
    }

    [Fact]
    public async Task LoadAsync_Collection_EntityIdZero_SkipsLoading()
    {
        using var db = TestDb.Create();
        var post = new Post { Id = 0 };
        db.Posts.Attach(post);

        var ex = await Record.ExceptionAsync(
            () => db.LoadAsync<Post, Tag>(post, p => p.Tags));
        Assert.Null(ex);
    }

    // ── LoadAsync (actual loading via InMemory) ───────────────────────────────

    [Fact]
    public async Task LoadAsync_Reference_LoadsNavigationProperty()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using (var db1 = new TestDbContext(options))
        {
            var author = new Author { Name = "Paul" };
            var post = new Post { Title = "Romans", Author = author };
            db1.Posts.Add(post);
            await db1.SaveChangesAsync();
        }

        using (var db2 = new TestDbContext(options))
        {
            var freshPost = await db2.Posts.FindAsync(1); // or attach new Post { Id = 1 }

            await db2.LoadAsync<Post, Author>(freshPost!, p => p.Author!);

            Assert.True(db2.Entry(freshPost!).Reference(p => p.Author).IsLoaded);
        }
    }

    [Fact]
    public async Task LoadAsync_Collection_LoadsNavigationProperty()
    {
        using var db = TestDb.Create();
        var post = new Post { Title = "Galatians" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        db.Tags.AddRange(
            new Tag { Name = "Faith", PostId = post.Id },
            new Tag { Name = "Law", PostId = post.Id });
        await db.SaveChangesAsync();

        // Detach so collection isn't cached, then reload the post
        db.ChangeTracker.Clear();
        var freshPost = await db.Posts.FindAsync(post.Id);

        await db.LoadAsync<Post, Tag>(freshPost!, p => p.Tags);

        Assert.True(db.Entry(freshPost!).Collection(p => p.Tags).IsLoaded);
        Assert.Equal(2, freshPost!.Tags.Count);
    }

    [Fact]
    public async Task LoadAsync_CollectionEntities_SkipsZeroIdEntities()
    {
        using var db = TestDb.Create();
        var saved = new Post { Id = 1, Title = "Saved", IsLive = true };
        var unsaved = new Post { Id = 0, Title = "Unsaved", IsLive = true };

        // Both passed to the collection overload; Id=0 should be skipped
        var posts = new List<Post> { saved, unsaved };

        // Should not throw even though Id=0 entity is skipped
        var ex = await Record.ExceptionAsync(
            () => db.LoadAsync<Post, Tag>(posts, p => p.Tags));
        Assert.Null(ex);
    }
}