namespace CathermanCore.Test;

public class ExtensionsTests
{
    // ── HasValue (object) ─────────────────────────────────────────────────────

    [Fact] public void HasValue_NullObject_ReturnsFalse() => Assert.False(((object?)null).HasValue());
    [Fact] public void HasValue_NonNullObject_ReturnsTrue() => Assert.True(new object().HasValue());
    [Fact] public void HasValue_NullString_ReturnsFalse() => Assert.False(((string?)null).HasValue());
    [Fact] public void HasValue_EmptyString_ReturnsFalse() => Assert.False("".HasValue());
    [Fact] public void HasValue_WhitespaceString_ReturnsFalse() => Assert.False("   ".HasValue());
    [Fact] public void HasValue_NonEmptyString_ReturnsTrue() => Assert.True("hello".HasValue());
    [Fact] public void HasValue_EmptyList_ReturnsFalse() => Assert.False(((object)new List<int>()).HasValue());
    [Fact] public void HasValue_NonEmptyList_ReturnsTrue() => Assert.True(((object)new List<int> { 1 }).HasValue());

    // ── HasValue (int) ────────────────────────────────────────────────────────

    [Fact] public void HasValue_ZeroInt_ReturnsFalse() => Assert.False(0.HasValue());
    [Fact] public void HasValue_PositiveInt_ReturnsTrue() => Assert.True(1.HasValue());
    [Fact] public void HasValue_NegativeInt_ReturnsTrue() => Assert.True((-1).HasValue());
    [Fact] public void HasValue_NullableIntNull_ReturnsFalse() => Assert.False(((int?)null).HasValue());
    [Fact] public void HasValue_NullableIntZero_ReturnsFalse() => Assert.False(((int?)0).HasValue());
    [Fact] public void HasValue_NullableIntNonZero_ReturnsTrue() => Assert.True(((int?)5).HasValue());

    // ── HasNoValue ────────────────────────────────────────────────────────────

    [Fact] public void HasNoValue_NullObject_ReturnsTrue() => Assert.True(((object?)null).HasNoValue());
    [Fact] public void HasNoValue_NonNullObject_ReturnsFalse() => Assert.False(new object().HasNoValue());
    [Fact] public void HasNoValue_EmptyString_ReturnsTrue() => Assert.True("".HasNoValue());
    [Fact] public void HasNoValue_NonEmptyString_ReturnsFalse() => Assert.False("hello".HasNoValue());
    [Fact] public void HasNoValue_ZeroInt_ReturnsTrue() => Assert.True(0.HasNoValue());
    [Fact] public void HasNoValue_NonZeroInt_ReturnsFalse() => Assert.False(5.HasNoValue());

    // ── HasValue(IList<T>, Func) ──────────────────────────────────────────────

    [Fact]
    public void HasValue_ListWithPredicate_NullList_ReturnsFalse()
    {
        List<int>? list = null;
        Assert.False(list.HasValue(x => x > 0));
    }

    [Fact]
    public void HasValue_ListWithPredicate_EmptyList_ReturnsFalse()
    {
        Assert.False(new List<int>().HasValue(x => x > 0));
    }

    [Fact]
    public void HasValue_ListWithPredicate_NoMatch_ReturnsFalse()
    {
        Assert.False(new List<int> { -1, -2 }.HasValue(x => x > 0));
    }

    [Fact]
    public void HasValue_ListWithPredicate_HasMatch_ReturnsTrue()
    {
        Assert.True(new List<int> { -1, 5, -2 }.HasValue(x => x > 0));
    }

    // ── AddIfMissing ──────────────────────────────────────────────────────────

    [Fact]
    public void AddIfMissing_NewItem_AddsIt()
    {
        var list = new List<int> { 1, 2 };
        list.AddIfMissing(3);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void AddIfMissing_ExistingItem_DoesNotDuplicate()
    {
        var list = new List<int> { 1, 2, 3 };
        list.AddIfMissing(2);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void AddIfMissing_NullItem_IgnoresIt()
    {
        var list = new List<string?> { "a" };
        list.AddIfMissing((string?)null);
        Assert.Single(list);
    }

    [Fact]
    public void AddIfMissing_IEnumerable_AddsOnlyMissing()
    {
        var list = new List<int> { 1, 2 };
        list.AddIfMissing(new[] { 2, 3, 4 });
        Assert.Equal(new[] { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void AddIfMissing_IEnumerable_AllExisting_NoChange()
    {
        var list = new List<int> { 1, 2, 3 };
        list.AddIfMissing(new[] { 1, 2, 3 });
        Assert.Equal(3, list.Count);
    }

    // ── MoveToFront ───────────────────────────────────────────────────────────

    [Fact]
    public void MoveToFront_ItemExists_MovesToIndex0()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        list.MoveToFront(x => x == 3);
        Assert.Equal(3, list[0]);
        Assert.Equal(4, list.Count);
    }

    [Fact]
    public void MoveToFront_ItemAlreadyAtFront_NoChange()
    {
        var list = new List<int> { 1, 2, 3 };
        list.MoveToFront(x => x == 1);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void MoveToFront_ItemNotFound_NoChange()
    {
        var list = new List<int> { 1, 2, 3 };
        list.MoveToFront(x => x == 99);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    // ── Pop ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Pop_NonEmptyList_ReturnsFirstAndRemoves()
    {
        var list = new List<int> { 10, 20, 30 };
        var result = list.Pop();
        Assert.Equal(10, result);
        Assert.Equal(new[] { 20, 30 }, list);
    }

    [Fact]
    public void Pop_EmptyList_ReturnsDefault()
    {
        var list = new List<int>();
        var result = list.Pop();
        Assert.Equal(default, result);
    }

    [Fact]
    public void Pop_SingleItem_ReturnsItemAndLeavesEmpty()
    {
        var list = new List<string> { "only" };
        var result = list.Pop();
        Assert.Equal("only", result);
        Assert.Empty(list);
    }

    // ── SplitAndTrim ──────────────────────────────────────────────────────────

    [Fact]
    public void SplitAndTrim_RemovesWhitespaceAndEmptyEntries()
    {
        var result = "a , b , , c".SplitAndTrim(",");
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    [Fact]
    public void SplitAndTrim_EmptyString_ReturnsEmptyList()
    {
        Assert.Empty("".SplitAndTrim(","));
    }

    [Fact]
    public void SplitAndTrim_NoDelimiter_ReturnsSingleItem()
    {
        var result = "hello".SplitAndTrim(",");
        Assert.Single(result);
        Assert.Equal("hello", result[0]);
    }

    // ── FirstCharToUpper ──────────────────────────────────────────────────────

    [Fact] public void FirstCharToUpper_Lowercase_Capitalizes() => Assert.Equal("Hello", "hello".FirstCharToUpper());
    [Fact] public void FirstCharToUpper_AllCaps_CapitalizesFirst() => Assert.Equal("Hello", "HELLO".FirstCharToUpper());
    [Fact] public void FirstCharToUpper_EmptyString_ReturnsEmpty() => Assert.Equal("", "".FirstCharToUpper());
    [Fact] public void FirstCharToUpper_SingleChar_Capitalizes() => Assert.Equal("A", "a".FirstCharToUpper());

    // ── RemoveDigits / GetNumbers ─────────────────────────────────────────────

    [Fact]
    public void RemoveDigits_RemovesAllDigits()
    {
        Assert.Equal("abc", "a1b2c3".RemoveDigits());
    }

    [Fact]
    public void RemoveDigits_NoDigits_Unchanged()
    {
        Assert.Equal("abc", "abc".RemoveDigits());
    }

    [Fact]
    public void GetNumbers_ExtractsNumbers()
    {
        var numbers = "John 3 16 Romans 8 28".GetNumbers();
        Assert.Equal(new[] { 3, 16, 8, 28 }, numbers);
    }

    [Fact]
    public void GetNumbers_NoNumbers_ReturnsEmpty()
    {
        Assert.Empty("hello world".GetNumbers());
    }

    // ── ToBase64 / FromBase64 ─────────────────────────────────────────────────

    [Fact]
    public void ToBase64_FromBase64_Roundtrip()
    {
        var original = "Hello, World!";
        Assert.Equal(original, original.ToBase64().FromBase64());
    }

    [Fact]
    public void ToBase64_EmptyString_Roundtrips()
    {
        Assert.Equal("", "".ToBase64().FromBase64());
    }

    [Fact]
    public void ToBase64_FromBase64_TypedRoundtrip()
    {
        var original = new { Name = "Test", Value = 42 };
        var json = original.ToJson();
        var encoded = json.ToBase64();
        var decoded = encoded.FromBase64();
        Assert.Equal(json, decoded);
    }

    // ── IsSimpleType ──────────────────────────────────────────────────────────

    [Fact] public void IsSimpleType_Int_ReturnsTrue() => Assert.True(typeof(int).IsSimpleType());
    [Fact] public void IsSimpleType_Bool_ReturnsTrue() => Assert.True(typeof(bool).IsSimpleType());
    [Fact] public void IsSimpleType_String_ReturnsTrue() => Assert.True(typeof(string).IsSimpleType());
    [Fact] public void IsSimpleType_DateTime_ReturnsTrue() => Assert.True(typeof(DateTime).IsSimpleType());
    [Fact] public void IsSimpleType_Class_ReturnsFalse() => Assert.False(typeof(List<int>).IsSimpleType());
    [Fact] public void IsSimpleType_Object_ReturnsFalse() => Assert.False(typeof(object).IsSimpleType());

    // ── IsList / IsDictionary ─────────────────────────────────────────────────

    [Fact] public void IsList_ListObject_ReturnsTrue() => Assert.True(new List<int>().IsList());
    [Fact] public void IsList_NonList_ReturnsFalse() => Assert.False("hello".IsList());
    [Fact] public void IsList_Null_ReturnsFalse() => Assert.False(((object?)null)!.IsList());
    [Fact] public void IsDictionary_DictObject_ReturnsTrue() => Assert.True(new Dictionary<string, int>().IsDictionary());
    [Fact] public void IsDictionary_NonDict_ReturnsFalse() => Assert.False(new List<int>().IsDictionary());

    // ── Column ────────────────────────────────────────────────────────────────

    [Fact]
    public void Column_EvenSplit_ReturnsCorrectChunk()
    {
        var list = Enumerable.Range(0, 9).ToList();
        Assert.Equal(new[] { 0, 1, 2 }, list.Column(0, 3));
        Assert.Equal(new[] { 3, 4, 5 }, list.Column(1, 3));
        Assert.Equal(new[] { 6, 7, 8 }, list.Column(2, 3));
    }

    [Fact]
    public void Column_UnevenSplit_LastColumnShorter()
    {
        var list = Enumerable.Range(0, 10).ToList();
        var col0 = list.Column(0, 3); // count=4: [0,1,2,3]
        var col1 = list.Column(1, 3); // skip=4: [4,5,6,7]
        var col2 = list.Column(2, 3); // skip=8: [8,9] (only 2 left)

        Assert.Equal(4, col0.Count);
        Assert.Equal(4, col1.Count);
        Assert.Equal(2, col2.Count);
    }

    // ── HaveSameUniqueInts ────────────────────────────────────────────────────

    [Fact]
    public void HaveSameUniqueInts_SameLists_ReturnsTrue() =>
        Assert.True(new[] { 1, 2, 3 }.HaveSameUniqueInts(new[] { 3, 2, 1 }));

    [Fact]
    public void HaveSameUniqueInts_DifferentLists_ReturnsFalse() =>
        Assert.False(new[] { 1, 2, 3 }.HaveSameUniqueInts(new[] { 1, 2, 4 }));

    [Fact]
    public void HaveSameUniqueInts_DuplicatesInSource_TreatedAsSet() =>
        Assert.True(new[] { 1, 1, 2 }.HaveSameUniqueInts(new[] { 1, 2 }));

    [Fact]
    public void HaveSameUniqueInts_BothNull_ReturnsTrue() =>
        Assert.True(((IEnumerable<int>?)null).HaveSameUniqueInts(null));

    [Fact]
    public void HaveSameUniqueInts_OneNull_ReturnsFalse() =>
        Assert.False(new[] { 1 }.HaveSameUniqueInts(null));

    // ── ContainsSubstringTwice ────────────────────────────────────────────────

    [Fact]
    public void ContainsSubstringTwice_AppearsTwice_ReturnsTrue() =>
        Assert.True("hello hello world".ContainsSubstringTwice("hello"));

    [Fact]
    public void ContainsSubstringTwice_AppearsOnce_ReturnsFalse() =>
        Assert.False("hello world".ContainsSubstringTwice("hello"));

    [Fact]
    public void ContainsSubstringTwice_AppearsNever_ReturnsFalse() =>
        Assert.False("hello world".ContainsSubstringTwice("xyz"));

    // ── GetSubstrings ─────────────────────────────────────────────────────────

    [Fact]
    public void GetSubstrings_ExtractsTokensBetweenDelimiters()
    {
        var result = "{hello} {world}".GetSubstrings("{", "}");
        Assert.Equal(new[] { "hello", "world" }, result);
    }

    [Fact]
    public void GetSubstrings_NoMatches_ReturnsEmpty()
    {
        Assert.Empty("hello world".GetSubstrings("{", "}"));
    }

    // ── ToIndexDictionary ─────────────────────────────────────────────────────

    [Fact]
    public void ToIndexDictionary_MapsIndexToValue()
    {
        var dict = new[] { "a", "b", "c" }.ToIndexDictionary();
        Assert.Equal("a", dict[0]);
        Assert.Equal("b", dict[1]);
        Assert.Equal("c", dict[2]);
    }

    // ── CapitalizeEveryWord ───────────────────────────────────────────────────

    [Fact]
    public void CapitalizeEveryWord_AllLower_TitleCases() =>
        Assert.Equal("Hello World", "hello world".CapitalizeEveryWord());

    [Fact]
    public void CapitalizeEveryWord_AllUpper_TitleCases() =>
        Assert.Equal("Hello World", "HELLO WORLD".CapitalizeEveryWord());

    // ── RemoveLetters / GetLetters ────────────────────────────────────────────

    [Fact]
    public void RemoveLetters_LeavesOnlySymbolsAndDigits() =>
        Assert.Equal("3:16", "John3:16".RemoveLetters());

    [Fact]
    public void GetLetters_LeavesOnlyLetters() =>
        Assert.Equal("John", "John3:16".GetLetters());

    // ── NotAny ────────────────────────────────────────────────────────────────

    [Fact]
    public void NotAny_NoneMatch_ReturnsTrue() =>
        Assert.True(new[] { 1, 2, 3 }.NotAny(x => x > 10));

    [Fact]
    public void NotAny_SomeMatch_ReturnsFalse() =>
        Assert.False(new[] { 1, 2, 11 }.NotAny(x => x > 10));

    // ── GetOrCreate ───────────────────────────────────────────────────────────

    [Fact]
    public void GetOrCreate_KeyExists_ReturnsExistingValue()
    {
        var dict = new Dictionary<string, int> { ["a"] = 42 };
        Assert.Equal(42, dict.GetOrCreate("a", () => 99));
    }

    [Fact]
    public void GetOrCreate_KeyMissing_AddsAndReturnsNew()
    {
        var dict = new Dictionary<string, int>();
        var result = dict.GetOrCreate("a", () => 99);
        Assert.Equal(99, result);
        Assert.True(dict.ContainsKey("a"));
    }

    // ── AddOrReplaceEntity ───────────────────────────────────────────────────

    private class TestEntity : IHasId<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Fact]
    public void AddOrReplaceEntity_NewId_AddsItem()
    {
        var list = new List<TestEntity> { new() { Id = 1, Name = "First" } };
        list.AddOrReplaceEntity<TestEntity, int>(new TestEntity { Id = 2, Name = "Second" });
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void AddOrReplaceEntity_ExistingId_ReplacesItem()
    {
        var list = new List<TestEntity> { new() { Id = 1, Name = "Old" } };
        list.AddOrReplaceEntity<TestEntity, int>(new TestEntity { Id = 1, Name = "New" });
        Assert.Single(list);
        Assert.Equal("New", list[0].Name);
    }

    // ── AllIdsMatch ───────────────────────────────────────────────────────────

    [Fact]
    public void AllIdsMatch_AllPresent_ReturnsTrue()
    {
        var list = new List<TestEntity>
        {
            new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 }
        };
        Assert.True(list.AllIdsMatch<TestEntity, int>(new[] { 1, 2, 3 }));
    }

    [Fact]
    public void AllIdsMatch_CountMismatch_ReturnsFalse()
    {
        var list = new List<TestEntity> { new() { Id = 1 } };
        Assert.False(list.AllIdsMatch<TestEntity, int>(new[] { 1, 2 }));
    }

    [Fact]
    public void AllIdsMatch_DifferentIds_ReturnsFalse()
    {
        var list = new List<TestEntity> { new() { Id = 1 }, new() { Id = 2 } };
        Assert.False(list.AllIdsMatch<TestEntity, int>(new[] { 1, 3 }));
    }

    [Fact]
    public void AllIdsMatch_EmptyList_ReturnsFalse()
    {
        Assert.False(new List<TestEntity>().AllIdsMatch<TestEntity, int>(new[] { 1 }));
    }
}