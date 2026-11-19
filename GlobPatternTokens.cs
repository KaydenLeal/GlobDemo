namespace GlobDemo;

/// <summary>
///     A single token in a <see cref="GlobPattern" />.
/// </summary>
public readonly union GlobPatternToken(GlobTokenLiteral, GlobTokenAnyChar, GlobTokenAnySequence, GlobTokenCharacterSet);

/// <summary>
///     Glob pattern token that matches an exact character once.
/// </summary>
/// <param name="character">The character this token matches.</param>
public class GlobTokenLiteral(char character)
{
    /// <summary>
    ///     The character this token matches.
    /// </summary>
    public char Character = character;
}

/// <summary>
///     Glob pattern token that matches any character once.
/// </summary>
public class GlobTokenAnyChar;

/// <summary>
///     Glob pattern token that matches a sequence of zero or more characters.
/// </summary>
public class GlobTokenAnySequence;

/// <summary>
///     Glob pattern token that matches a set of characters, or against a set of characters if negated.
///     <para></para>
///     The set is a tokenized list of <see cref="GlobCharacterSetToken" />s.
/// </summary>
/// <param name="negated">Negate the result of matching this character set.</param>
/// <param name="tokens">Flat list containing this set's tokens in order.</param>
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
///     included in a range between <paramref name="from" /> and <paramref name="to" />.
/// </summary>
/// <param name="from">Inclusive character range start.</param>
/// <param name="to">Inclusive character range end.</param>
public class GlobTokenRange(char from, char to)
{
    /// <summary>
    ///     Inclusive character range start.
    /// </summary>
    public char From = from;

    /// <summary>
    ///     Inclusive character range end.
    /// </summary>
    public char To = to;
}
