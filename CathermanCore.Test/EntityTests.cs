namespace CathermanCore.Test;

public class EntityTests
{
    // Minimal concrete Entity for testing
    private class TestEntity : Entity
    {
        public TestEntity(int id = 0) => Id = id;
    }

    // ── Defaults ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsLive_DefaultsToTrue()
    {
        Assert.True(new TestEntity().IsLive);
    }

    [Fact]
    public void LastUpdateBy_DefaultsToEmptyString()
    {
        Assert.Equal("", new TestEntity().LastUpdateBy);
    }

    [Fact]
    public void SourceData_DefaultsToNA()
    {
        Assert.Equal("N/A", new TestEntity().SourceData);
    }

    [Fact]
    public void Id_DefaultsToZero()
    {
        Assert.Equal(0, new TestEntity().Id);
    }

    // ── Equals ────────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_SamePositiveId_ReturnsTrue()
    {
        Assert.Equal(new TestEntity(1), new TestEntity(1));
    }

    [Fact]
    public void Equals_DifferentIds_ReturnsFalse()
    {
        Assert.NotEqual(new TestEntity(1), new TestEntity(2));
    }

    [Fact]
    public void Equals_BothIdZero_ReturnsFalse()
    {
        // Unsaved entities are never considered equal
        Assert.NotEqual(new TestEntity(0), new TestEntity(0));
    }

    [Fact]
    public void Equals_OneIdZero_ReturnsFalse()
    {
        Assert.NotEqual(new TestEntity(0), new TestEntity(1));
    }

    [Fact]
    public void Equals_NullOther_ReturnsFalse()
    {
        Assert.False(new TestEntity(1).Equals(null));
    }

    [Fact]
    public void Equals_DifferentEntityType_SameId_ReturnsTrue()
    {
        // Two different Entity subclasses with same Id are considered equal
        // (base Equals only checks Id, not type)
        var a = new TestEntity(5);
        var b = new AnotherEntity(5);
        Assert.True(a.Equals(b));
    }

    // ── GetHashCode ───────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        Assert.Equal(new TestEntity(1).GetHashCode(), new TestEntity(1).GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentIds_GenerallyDifferent()
    {
        // Not guaranteed by contract but true for int hash codes
        Assert.NotEqual(new TestEntity(1).GetHashCode(), new TestEntity(2).GetHashCode());
    }

    [Fact]
    public void GetHashCode_IsBasedOnIdNotMutableFields()
    {
        var entity = new TestEntity(1);
        var hashBefore = entity.GetHashCode();
        entity.IsLive = false;
        entity.LastUpdateBy = "someone";
        var hashAfter = entity.GetHashCode();
        Assert.Equal(hashBefore, hashAfter);
    }

    // ── Contract: Equals implies same HashCode ────────────────────────────────

    [Fact]
    public void EqualEntities_HaveSameHashCode()
    {
        var a = new TestEntity(42);
        var b = new TestEntity(42);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // ── IsValid ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_BaseImplementation_ReturnsTrue()
    {
        Assert.True(new TestEntity().IsValid());
    }

    // ── HashSet / Dictionary usage (depends on correct Equals + GetHashCode) ──

    [Fact]
    public void HashSet_DeduplicatesByEntityId()
    {
        var set = new HashSet<TestEntity>
        {
            new TestEntity(1),
            new TestEntity(1),  // duplicate
            new TestEntity(2)
        };
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void HashSet_ZeroIdEntities_AreNotDeduped()
    {
        // Unsaved entities (Id=0) should not deduplicate
        var set = new HashSet<TestEntity>
        {
            new TestEntity(0),
            new TestEntity(0)
        };
        Assert.Equal(2, set.Count);
    }

    private class AnotherEntity : Entity
    {
        public AnotherEntity(int id) => Id = id;
    }
}