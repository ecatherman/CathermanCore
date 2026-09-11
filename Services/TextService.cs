using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CathermanCore;

public static class TextService
{
    /// <summary>
    /// Determines whether a specific word sequence contains the target text sequence in order.
    /// </summary>
    /// <param name="wordSequence">The full text sequence to search within (e.g., "The quick brown fox").</param>
    /// <param name="text">The phrase or sequence of words to look for (e.g., "quick brown").</param>
    /// <returns><c>true</c> if the exact sequence of words exists within <paramref name="wordSequence"/>; otherwise, <c>false</c>.</returns>
    public static bool ContainsWordSequence(string wordSequence, string text) =>
        ContainsWordSequence(GetWords(wordSequence), text);

    /// <summary>
    /// Determines whether a target text contains an exact ordered word sequence from <paramref name="wordSequence"/>.
    /// Falls back to checking if <paramref name="text"/> contains any individual word from <paramref name="wordSequence"/>.
    /// </summary>
    /// <param name="wordSequence">A pre-split list of words representing the target search space.</param>
    /// <param name="text">The phrase or sequence of words to look for in <paramref name="wordSequence"/>.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="text"/> produces normalized words that match an exact ordered 
    /// sub-sequence inside <paramref name="wordSequence"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Words are compared case-insensitively and without punctuation due to internal normalization of <paramref name="text"/>.
    /// </remarks>
    public static bool ContainsWordSequence(List<string> wordSequence, string text)
    {
        if (wordSequence.HasNoValue() || text.HasNoValue())
            return false;

        var textWords = GetWords(text);

        if (textWords.HasNoValue())
            return false;

        // Clean and normalize the search sequence
        var normalizedSequence = wordSequence
            .Select(w => NormalizeText(w))
            .Where(w => w.HasValue())
            .ToList();

        if (!normalizedSequence.Any())
            return false;

        // 1. Try full contiguous sequence match first
        if (normalizedSequence.Count <= textWords.Count)
        {
            for (int i = 0; i <= textWords.Count - normalizedSequence.Count; i++)
            {
                bool match = true;

                for (int j = 0; j < normalizedSequence.Count; j++)
                {
                    if (!string.Equals(textWords[i + j], normalizedSequence[j], StringComparison.OrdinalIgnoreCase))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return true;
            }
        }

        // 2. Fallback: Check if text contains any individual word from wordSequence
        var textWordSet = new HashSet<string>(textWords, StringComparer.OrdinalIgnoreCase);
        return normalizedSequence.Any(word => textWordSet.Contains(word));
    }

    /// <summary>
    /// Finds non-overlapping matches from a collection of candidate items ordered by length descending.
    /// Prefers longer sequence matches and claims word indices to prevent smaller sub-phrases from matching.
    /// </summary>
    /// <typeparam name="T">The type containing candidate metadata (e.g., Id, Name).</typeparam>
    /// <param name="text">The source text to search against.</param>
    /// <param name="candidates">The available candidate items.</param>
    /// <param name="textSelector">Selector to extract the phrase string from each candidate item.</param>
    /// <returns>A list of matching candidate items.</returns>
    public static List<T> MatchNonOverlappingSequences<T>(string text, IEnumerable<T> candidates, Func<T, string> textSelector)
    {
        if (text.HasNoValue() || candidates.HasNoValue() || textSelector.HasNoValue())
            return [];

        var textWords = GetWords(text);
        if (!textWords.Any())
            return [];

        // Pre-tokenize candidates and sort by word length descending so longer phrases claim indices first
        var sortedCandidates = candidates
            .Select(item => new
            {
                Item = item,
                Words = GetWords(textSelector(item))
            })
            .Where(c => c.Words.Any())
            .OrderByDescending(c => c.Words.Count)
            .ToList();

        var matchedItems = new List<T>();
        var usedIndices = new HashSet<int>();

        foreach (var candidate in sortedCandidates)
        {
            var matchPositions = FindSequenceIndices(textWords, candidate.Words);
            bool matched = false;

            foreach (int startIdx in matchPositions)
            {
                var wordRange = Enumerable.Range(startIdx, candidate.Words.Count);

                // If any word index in this sequence was already claimed by a longer match, skip it
                if (wordRange.Any(usedIndices.Contains))
                    continue;

                // Claim word indices
                foreach (int idx in wordRange)
                    usedIndices.Add(idx);

                matched = true;
            }

            if (matched)
                matchedItems.Add(candidate.Item);
        }

        return matchedItems;
    }

    /// <summary>
    /// Finds all zero-based starting word indices where targetWords matches contiguous sequence in textWords.
    /// </summary>
    private static List<int> FindSequenceIndices(List<string> textWords, List<string> targetWords)
    {
        var startingIndices = new List<int>();

        if (targetWords == null || targetWords.Count == 0 || targetWords.Count > textWords.Count)
            return startingIndices;

        for (int i = 0; i <= textWords.Count - targetWords.Count; i++)
        {
            bool match = true;

            for (int j = 0; j < targetWords.Count; j++)
            {
                if (!string.Equals(textWords[i + j], targetWords[j], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }

            if (match)
                startingIndices.Add(i);
        }

        return startingIndices;
    }

    /// <summary>
    /// Normalizes an input string and splits it into a list of individual cleaned words.
    /// </summary>
    /// <param name="text">The raw string to tokenize into words.</param>
    /// <returns>A <see cref="List{T}"/> of strings containing the cleaned, lowercase individual words.</returns>
    /// <example>
    /// <code>
    /// List&lt;string&gt; words = GetWords("[Intro] Hello, world!"); 
    /// // Returns: ["hello", "world"]
    /// </code>
    /// </example>
    public static List<string> GetWords(string text)
    {
        text = NormalizeText(text);

        return text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }


    private static readonly Regex BracketPattern = new(@"\[[^\]]*\]", RegexOptions.Compiled);
    private static readonly Regex PunctuationPattern = new(@"[^\w\s]", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes input text by stripping bracketed content, removing punctuation, converting to lower case, 
    /// and collapsing whitespace.
    /// </summary>
    /// <param name="input">The raw text to clean and normalize.</param>
    /// <returns>A sanitized string stripped of special characters, brackets, and redundant spaces.</returns>
    /// <remarks>
    /// Execution order:
    /// <list type="number">
    ///   <item><description>Removes bracketed text including brackets (e.g., <c>[Verse 1]</c> → <c>""</c>).</description></item>
    ///   <item><description>Converts characters to lower-case using invariant culture rules.</description></item>
    ///   <item><description>Strips out all non-word, non-whitespace characters (punctuation/symbols).</description></item>
    ///   <item><description>Collapses multiple contiguous whitespace characters into a single space and trims edges.</description></item>
    /// </list>
    /// </remarks>
    public static string NormalizeText(string input)
    {
        input = BracketPattern.Replace(input, "");
        input = input.ToLowerInvariant();
        input = PunctuationPattern.Replace(input, "");
        input = WhitespacePattern.Replace(input, " ");
        return input.Trim();
    }
}
