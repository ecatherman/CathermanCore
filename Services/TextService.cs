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
    /// Determines whether a tokenized word list contains the exact target text sequence in order.
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
        if (text.HasNoValue())
            return false;

        var subjectWords = GetWords(text);

        if (!subjectWords.Any())
            return false;

        for (int i = 0; i <= wordSequence.Count - subjectWords.Count; i++)
        {
            bool match = true;

            for (int j = 0; j < subjectWords.Count; j++)
            {
                if (wordSequence[i + j] != subjectWords[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return true;
        }

        return false;
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
