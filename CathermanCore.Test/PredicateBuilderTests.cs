using System.Linq.Expressions;

namespace CathermanCore.Test;

public class PredicateBuilderTests
{
    private record Item(int Value, string Tag);

    private static List<Item> Items =>
    [
        new(1, "odd"),
        new(2, "even"),
        new(3, "odd"),
        new(4, "even"),
        new(5, "odd"),
    ];

    private static Func<Item, bool> Compile(Expression<Func<Item, bool>> expr) =>
        expr.Compile();

    // ── True / False ─────────────────────────────────────────────────────────

    [Fact]
    public void True_ReturnsTrueForAllItems()
    {
        var predicate = Compile(PredicateBuilder.True<Item>());
        Assert.True(Items.All(predicate));
    }

    [Fact]
    public void False_ReturnsFalseForAllItems()
    {
        var predicate = Compile(PredicateBuilder.False<Item>());
        Assert.True(Items.All(x => !predicate(x)));
    }

    // ── Or ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Or_BothFalse_ReturnsFalse()
    {
        var expr = PredicateBuilder.False<Item>().Or(_ => false);
        var predicate = Compile(expr);
        Assert.DoesNotContain(Items, x => predicate(x));
    }

    [Fact]
    public void Or_FirstTrue_ReturnsTrue()
    {
        var expr = PredicateBuilder.True<Item>().Or(_ => false);
        var predicate = Compile(expr);
        Assert.Equal(Items.Count, Items.Count(predicate));
    }

    [Fact]
    public void Or_SecondTrue_ReturnsTrue()
    {
        var expr = PredicateBuilder.False<Item>().Or(_ => true);
        var predicate = Compile(expr);
        Assert.Equal(Items.Count, Items.Count(predicate));
    }

    [Fact]
    public void Or_CombinesTwoConditions()
    {
        // Items where value == 1 OR tag == "even"
        Expression<Func<Item, bool>> isOne = x => x.Value == 1;
        Expression<Func<Item, bool>> isEven = x => x.Tag == "even";

        var predicate = Compile(isOne.Or(isEven));
        var results = Items.Where(predicate).ToList();

        Assert.Equal(3, results.Count); // 1, 2, 4
        Assert.Contains(results, x => x.Value == 1);
        Assert.Contains(results, x => x.Value == 2);
        Assert.Contains(results, x => x.Value == 4);
    }

    // ── And ───────────────────────────────────────────────────────────────────

    [Fact]
    public void And_BothTrue_ReturnsTrue()
    {
        var expr = PredicateBuilder.True<Item>().And(_ => true);
        var predicate = Compile(expr);
        Assert.Equal(Items.Count, Items.Count(predicate));
    }

    [Fact]
    public void And_FirstFalse_ReturnsFalse()
    {
        var expr = PredicateBuilder.False<Item>().And(_ => true);
        var predicate = Compile(expr);
        Assert.DoesNotContain(Items, x => predicate(x));
    }

    [Fact]
    public void And_SecondFalse_ReturnsFalse()
    {
        var expr = PredicateBuilder.True<Item>().And(_ => false);
        var predicate = Compile(expr);
        Assert.DoesNotContain(Items, x => predicate(x));
    }

    [Fact]
    public void And_CombinesTwoConditions()
    {
        // Items where value > 2 AND tag == "odd"
        Expression<Func<Item, bool>> greaterThanTwo = x => x.Value > 2;
        Expression<Func<Item, bool>> isOdd = x => x.Tag == "odd";

        var predicate = Compile(greaterThanTwo.And(isOdd));
        var results = Items.Where(predicate).ToList();

        Assert.Equal(2, results.Count); // 3, 5
        Assert.All(results, x => Assert.True(x.Value > 2 && x.Tag == "odd"));
    }

    // ── Chaining ──────────────────────────────────────────────────────────────

    [Fact]
    public void And_ChainedMultipleTimes_AppliesAll()
    {
        Expression<Func<Item, bool>> pred = PredicateBuilder.True<Item>()
            .And(x => x.Value > 1)
            .And(x => x.Value < 5)
            .And(x => x.Tag == "even");

        var results = Items.Where(Compile(pred)).ToList();
        Assert.Equal(new[] { 2, 4 }, results.Select(x => x.Value));
    }

    [Fact]
    public void Or_ChainedMultipleTimes_AppliesAll()
    {
        Expression<Func<Item, bool>> pred = PredicateBuilder.False<Item>()
            .Or(x => x.Value == 1)
            .Or(x => x.Value == 3)
            .Or(x => x.Value == 5);

        var results = Items.Where(Compile(pred)).ToList();
        Assert.Equal(3, results.Count);
        Assert.All(results, x => Assert.Equal("odd", x.Tag));
    }

    [Fact]
    public void Or_Then_And_ComplexComposition()
    {
        // (value == 1 OR value == 2) AND tag == "even"
        Expression<Func<Item, bool>> isOne = x => x.Value == 1;
        Expression<Func<Item, bool>> isTwo = x => x.Value == 2;
        Expression<Func<Item, bool>> isEven = x => x.Tag == "even";

        var pred = isOne.Or(isTwo).And(isEven);
        var results = Items.Where(Compile(pred)).ToList();

        // Only value==2 satisfies (1 OR 2) AND even
        Assert.Single(results);
        Assert.Equal(2, results[0].Value);
    }

    // ── Works with LINQ to Objects ────────────────────────────────────────────

    [Fact]
    public void PredicateBuilder_WorksWithLinqWhere()
    {
        Expression<Func<Item, bool>> pred =
            PredicateBuilder.False<Item>().Or(x => x.Value == 4);

        var results = Items.AsQueryable().Where(pred).ToList();
        Assert.Single(results);
        Assert.Equal(4, results[0].Value);
    }
}