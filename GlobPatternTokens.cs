namespace GlobDemo;

/// <summary>
///     A single token in a <see cref="GlobPattern" />.
/// </summary>
public readonly union GlobPatternToken(GlobTokenLiteral, GlobTokenAnyChar, GlobTokenAnySequence, GlobTokenCharacterSet);

/// <summary>
///     Matches an exact character once.
/// </summary>
public class GlobTokenLiteral(char character)
{
    /// <summary>
    ///     The character this token matches.
    /// </summary>
    public char Character = character;
}

/// <summary>
///     Matches any character once.
/// </summary>
public class GlobTokenAnyChar;

/// <summary>
///     Matches a sequence of zero or more characters.
/// </summary>
public class GlobTokenAnySequence;

/// <summary>
///     Matches one character if it matches any token in this set. When negated,
///     matches one character if it matches none of the tokens in this set.
/// </summary>
public class GlobTokenCharacterSet(bool negated, List<GlobCharacterSetToken> tokens)
{
    /// <summary>
    ///     Negate the result of matching this character set.
    /// </summary>
    public bool Negated = negated;

    /// <summary>
    ///     Flat list containing this set's tokens in order.
    /// </summary>
    public List<GlobCharacterSetToken> Tokens = tokens;
}

/// <summary>
///     A single token in a <see cref="GlobTokenCharacterSet" />.
/// </summary>
public readonly union GlobCharacterSetToken(GlobTokenLiteral, GlobTokenRange);

/// <summary>
///     Glob pattern token that matches if the input character is
///     included in a range between <paramref name="rangeStart" /> and <paramref name="rangeEnd" />.
/// </summary>
public class GlobTokenRange(char rangeStart, char rangeEnd)
{
    /// <summary>
    ///     Inclusive character range start.
    /// </summary>
    public char RangeStart = rangeStart;

    /// <summary>
    ///     Inclusive character range end.
    /// </summary>
    public char RangeEnd = rangeEnd;
}
