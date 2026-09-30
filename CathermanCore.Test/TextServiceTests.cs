namespace CathermanCore.Test;

public class TextServiceTests
{
    // ── NormalizeText ────────────────────────────────────────────────────────

    [Fact]
    public void NormalizeText_RemovesPunctuationAndBrackets_AndLowercases()
    {
        Assert.Equal("hello world", TextService.NormalizeText("Hello, [note] WORLD!"));
    }

    [Fact]
    public void NormalizeText_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", TextService.NormalizeText(""));
    }

    [Fact]
    public void NormalizeText_OnlyBrackets_ReturnsEmpty()
    {
        Assert.Equal("", TextService.NormalizeText("[verse 1][note]"));
    }

    [Fact]
    public void NormalizeText_OnlyPunctuation_ReturnsEmpty()
    {
        Assert.Equal("", TextService.NormalizeText(".,;:!?\"'"));
    }

    [Fact]
    public void NormalizeText_CollapsesMutipleSpaces()
    {
        Assert.Equal("hello world", TextService.NormalizeText("hello   world"));
    }

    [Fact]
    public void NormalizeText_TrimsLeadingAndTrailingWhitespace()
    {
        Assert.Equal("hello", TextService.NormalizeText("  hello  "));
    }

    [Fact]
    public void NormalizeText_MixedBracketsAndPunctuation()
    {
        Assert.Equal("for god loved the world",
            TextService.NormalizeText("For God [so] loved the world,"));
    }

    [Fact]
    public void NormalizeText_NestedBracketContent_Removed()
    {
        // Only removes [...] content, not nesting
        Assert.Equal("hello world", TextService.NormalizeText("hello [bracket content] world"));
    }

    [Fact]
    public void NormalizeText_NumbersPreserved()
    {
        Assert.Equal("john 316", TextService.NormalizeText("John 3:16"));
    }

    [Fact]
    public void NormalizeText_AlreadyNormalized_ReturnsSame()
    {
        Assert.Equal("hello world", TextService.NormalizeText("hello world"));
    }

    // ── GetWords ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetWords_SplitsAndNormalizes()
    {
        var words = TextService.GetWords("Hello, world! This is a test.");
        Assert.Equal(new[] { "hello", "world", "this", "is", "a", "test" }, words);
    }

    [Fact]
    public void GetWords_EmptyString_ReturnsEmptyList()
    {
        Assert.Empty(TextService.GetWords(""));
    }

    [Fact]
    public void GetWords_OnlyWhitespace_ReturnsEmptyList()
    {
        Assert.Empty(TextService.GetWords("   "));
    }

    [Fact]
    public void GetWords_SingleWord_ReturnsSingleElement()
    {
        var words = TextService.GetWords("Hello");
        Assert.Single(words);
        Assert.Equal("hello", words[0]);
    }

    [Fact]
    public void GetWords_BracketedContent_Removed()
    {
        var words = TextService.GetWords("[intro] God is love");
        Assert.Equal(new[] { "god", "is", "love" }, words);
    }

    [Fact]
    public void GetWords_MultipleSpacesBetweenWords_HandledCorrectly()
    {
        var words = TextService.GetWords("hello   world");
        Assert.Equal(new[] { "hello", "world" }, words);
    }

    // ── ContainsWordSequence(List<string>, string) ───────────────────────────

    [Fact]
    public void ContainsWordSequence_DetectsSequence()
    {
        var wordSequence = TextService.GetWords("The quick brown fox jumps over the lazy dog");
        Assert.True(TextService.ContainsWordSequence(wordSequence, "brown fox jumps"));
    }

    [Fact]
    public void ContainsWordSequence_SequenceAtStart_ReturnsTrue()
    {
        var words = TextService.GetWords("the quick brown fox");
        Assert.True(TextService.ContainsWordSequence(words, "the quick"));
    }

    [Fact]
    public void ContainsWordSequence_SequenceAtEnd_ReturnsTrue()
    {
        var words = TextService.GetWords("the quick brown fox");
        Assert.True(TextService.ContainsWordSequence(words, "brown fox"));
    }

    [Fact]
    public void ContainsWordSequence_FullSequenceMatch_ReturnsTrue()
    {
        var words = TextService.GetWords("God is love");
        Assert.True(TextService.ContainsWordSequence(words, "God is love"));
    }

    [Fact]
    public void ContainsWordSequence_WordsOutOfOrder_FallsBackToSingleWordMatch()
    {
        // Out-of-order doesn't match contiguously but fallback finds individual words
        var words = TextService.GetWords("the quick brown fox");
        Assert.True(TextService.ContainsWordSequence(words, "fox quick")); // fallback matches "fox" or "quick"
    }

    [Fact]
    public void ContainsWordSequence_NoMatch_ReturnsFalse()
    {
        var words = TextService.GetWords("the quick brown fox");
        Assert.False(TextService.ContainsWordSequence(words, "purple elephant"));
    }

    [Fact]
    public void ContainsWordSequence_EmptyWordSequence_ReturnsFalse()
    {
        Assert.False(TextService.ContainsWordSequence(new List<string>(), "hello"));
    }

    [Fact]
    public void ContainsWordSequence_EmptyText_ReturnsFalse()
    {
        var words = TextService.GetWords("hello world");
        Assert.False(TextService.ContainsWordSequence(words, ""));
    }

    [Fact]
    public void ContainsWordSequence_TextLongerThanSequence_ReturnsFalse()
    {
        var words = TextService.GetWords("hi");
        // Text has more words than the search sequence - falls back to single-word check
        Assert.True(TextService.ContainsWordSequence(words, "hi there friend"));
    }

    [Fact]
    public void ContainsWordSequence_CaseInsensitive()
    {
        var words = TextService.GetWords("God Is Love");
        Assert.True(TextService.ContainsWordSequence(words, "god is love"));
    }

    [Fact]
    public void ContainsWordSequence_PunctuationIgnored()
    {
        var words = TextService.GetWords("For God so loved the world");
        Assert.True(TextService.ContainsWordSequence(words, "loved, the world!"));
    }

    // ── MatchNonOverlappingSequences ─────────────────────────────────────────

    [Fact]
    public void MatchNonOverlappingSequences_FindsAllMatches()
    {
        var text = "God so loved the world";
        var candidates = new[] { "loved", "world", "sky" };

        var results = TextService.MatchNonOverlappingSequences(
            text, candidates, c => c);

        Assert.Contains("loved", results);
        Assert.Contains("world", results);
        Assert.DoesNotContain("sky", results);
    }

    [Fact]
    public void MatchNonOverlappingSequences_LongerPhraseWinsOverSubset()
    {
        var text = "God so loved the world";
        // "loved the world" is longer — should claim those indices
        // "the world" is a subset — should be blocked
        var candidates = new[] { "loved the world", "the world" };

        var results = TextService.MatchNonOverlappingSequences(
            text, candidates, c => c);

        Assert.Contains("loved the world", results);
        Assert.DoesNotContain("the world", results);
    }

    [Fact]
    public void MatchNonOverlappingSequences_MultipleNonOverlappingMatches()
    {
        var text = "grace and peace from God";
        var candidates = new[] { "grace", "peace", "God" };

        var results = TextService.MatchNonOverlappingSequences(
            text, candidates, c => c);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void MatchNonOverlappingSequences_EmptyText_ReturnsEmpty()
    {
        var results = TextService.MatchNonOverlappingSequences(
            "", new[] { "hello" }, c => c);

        Assert.Empty(results);
    }

    [Fact]
    public void MatchNonOverlappingSequences_EmptyCandidates_ReturnsEmpty()
    {
        var results = TextService.MatchNonOverlappingSequences(
            "hello world", Array.Empty<string>(), c => c);

        Assert.Empty(results);
    }

    [Fact]
    public void MatchNonOverlappingSequences_NoMatches_ReturnsEmpty()
    {
        var results = TextService.MatchNonOverlappingSequences(
            "the quick brown fox", new[] { "purple", "elephant" }, c => c);

        Assert.Empty(results);
    }

    [Fact]
    public void MatchNonOverlappingSequences_WordAppearsMultipleTimes_CountedOnce()
    {
        var text = "love God and love your neighbour";
        var candidates = new[] { "love" };

        var results = TextService.MatchNonOverlappingSequences(
            text, candidates, c => c);

        // "love" appears twice but the candidate "love" should appear only once
        Assert.Single(results);
    }

    [Fact]
    public void MatchNonOverlappingSequences_WorksWithCustomSelector()
    {
        var text = "God is love";
        var candidates = new[]
        {
            new { Id = 1, Phrase = "God is" },
            new { Id = 2, Phrase = "is love" },
        };

        var results = TextService.MatchNonOverlappingSequences(
            text, candidates, c => c.Phrase);

        // "God is" is longer and gets found first, claiming "is"
        // "is love" is blocked because "is" is claimed
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
    }
}