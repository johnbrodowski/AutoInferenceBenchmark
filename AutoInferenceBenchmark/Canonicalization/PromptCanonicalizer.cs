namespace AutoInferenceBenchmark.Canonicalization;

/// <summary>
/// Reduces noise introduced by linguistic variance in user input.
/// Applies configurable transformations to normalize phrasing before
/// clustering and optimization — while preserving the raw input for inference.
///
/// <para>LLMs are sensitive to phrasing differences that are semantically equivalent:
/// "will not" vs "won't", "four" vs "4", "do not" vs "don't". These differences
/// alter tokenization and probability distributions. Canonicalization normalizes
/// these for consistent configuration tracking.</para>
/// </summary>
public sealed class PromptCanonicalizer
{
    private readonly Dictionary<string, string> _transformations;

    /// <summary>Default contraction/expansion map for English text.</summary>
    public static readonly Dictionary<string, string> DefaultMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Contractions → expanded forms (improves multi-token consistency)
        ["won't"] = "will not",
        ["can't"] = "cannot",
        ["don't"] = "do not",
        ["doesn't"] = "does not",
        ["didn't"] = "did not",
        ["isn't"] = "is not",
        ["aren't"] = "are not",
        ["wasn't"] = "was not",
        ["weren't"] = "were not",
        ["hasn't"] = "has not",
        ["haven't"] = "have not",
        ["hadn't"] = "had not",
        ["wouldn't"] = "would not",
        ["shouldn't"] = "should not",
        ["couldn't"] = "could not",
        ["I'm"] = "I am",
        ["you're"] = "you are",
        ["they're"] = "they are",
        ["we're"] = "we are",
        ["it's"] = "it is",
        ["that's"] = "that is",
        ["what's"] = "what is",
        ["there's"] = "there is",
        ["let's"] = "let us",
        ["I've"] = "I have",
        ["you've"] = "you have",
        ["we've"] = "we have",
        ["they've"] = "they have",
        ["I'll"] = "I will",
        ["you'll"] = "you will",
        ["we'll"] = "we will",
        ["they'll"] = "they will",
        ["I'd"] = "I would",
        ["you'd"] = "you would",
        ["we'd"] = "we would",
        ["they'd"] = "they would",

        // Number words → digits (reduces tokenization variance)
        ["zero"] = "0",
        ["one"] = "1",
        ["two"] = "2",
        ["three"] = "3",
        ["four"] = "4",
        ["five"] = "5",
        ["six"] = "6",
        ["seven"] = "7",
        ["eight"] = "8",
        ["nine"] = "9",
        ["ten"] = "10"
    };

    public PromptCanonicalizer() : this(DefaultMap) { }

    public PromptCanonicalizer(Dictionary<string, string> transformations)
    {
        _transformations = new Dictionary<string, string>(transformations, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies all transformation rules to the input text.
    /// Uses word-boundary aware replacement to avoid partial matches.
    /// </summary>
    public string Canonicalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var result = input;
        foreach (var (from, to) in _transformations)
        {
            result = ReplaceWholeWord(result, from, to);
        }
        return result;
    }

    /// <summary>
    /// Adds or updates a transformation rule.
    /// </summary>
    public void AddRule(string from, string to) => _transformations[from] = to;

    /// <summary>
    /// Removes a transformation rule.
    /// </summary>
    public bool RemoveRule(string from) => _transformations.Remove(from);

    /// <summary>
    /// Returns a copy of the current transformation map.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetRules() =>
        new Dictionary<string, string>(_transformations);

    private static string ReplaceWholeWord(string text, string oldWord, string newWord)
    {
        int index = 0;
        while (index < text.Length)
        {
            int found = text.IndexOf(oldWord, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0) break;

            bool validBefore = found == 0 || !char.IsLetterOrDigit(text[found - 1]);
            int afterIdx = found + oldWord.Length;
            bool validAfter = afterIdx >= text.Length || !char.IsLetterOrDigit(text[afterIdx]);

            if (validBefore && validAfter)
            {
                text = string.Concat(text.AsSpan(0, found), newWord, text.AsSpan(afterIdx));
                index = found + newWord.Length;
            }
            else
            {
                index = found + 1;
            }
        }
        return text;
    }
}
