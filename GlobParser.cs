namespace GlobDemo;

/// <summary>
///     Parses strings into valid <see cref="GlobPattern" />s.
/// </summary>
public class GlobParser
{
    /// <summary>
    ///     Parsed token accumulator for <see cref="GlobParserState.Root" />.
    /// </summary>
    private readonly List<GlobPatternToken> _tokens = [];

    /// <summary>
    ///     Current parsing state.
    /// </summary>
    private GlobParserState _state = GlobParserState.Root;

    /// <summary>
    ///     Parsed token accumulator for <see cref="GlobParserState.Set" />.
    /// </summary>
    private List<GlobCharacterSetToken> _setTokens = [];

    /// <summary>
    ///     Current negated value for <see cref="GlobParserState.Set" />.
    /// </summary>
    private bool _setNegated;

    /// <summary>
    ///     Creates a new <see cref="GlobParser" /> instance and parses <paramref name="patternString" /> with it.
    /// </summary>
    /// <param name="patternString">String to parse as a <see cref="GlobPattern" />.</param>
    /// <returns>The parsed <see cref="GlobPattern" />.</returns>
    /// <exception cref="Exception">A parsing error occured.</exception>
    public static GlobPattern ParsePattern(string patternString)
    {
        var parser = new GlobParser();
        var charPos = 0;

        while (charPos < patternString.Length)
        {
            // Hand the parsing of this character to the proper state handler
            switch (parser._state)
            {
                case GlobParserState.Root:
                    parser.ParseOneRoot(patternString, ref charPos);
                    break;
                case GlobParserState.Set:
                    parser.ParseOneSet(patternString, ref charPos);
                    break;
            }

            charPos += 1;
        }

        return new GlobPattern(parser._tokens);
    }

    /// <summary>
    ///     Parses the current character in <paramref name="patternString" />
    ///     assuming the current state is <see cref="GlobParserState.Root" />.
    /// </summary>
    /// <param name="patternString">The full string being parsed.</param>
    /// <param name="charPos">Current character to parse.</param>
    /// <exception cref="Exception">A parsing error occured.</exception>
    private void ParseOneRoot(string patternString, ref int charPos)
    {
        char ch = patternString[charPos];

        if (ch == '\\')
        {
            charPos += 1;
            if (charPos >= patternString.Length) throw new Exception("Escape character is not followed by a character");
            char next = patternString[charPos];

            _tokens.Add(new GlobTokenLiteral(next));
            return;
        }

        if (ch == '?')
        {
            _tokens.Add(new GlobTokenAnyChar());
            return;
        }

        if (ch == '*')
        {
            _tokens.Add(new GlobTokenAnySequence());
            return;
        }

        if (ch == '[')
        {
            _state = GlobParserState.Set;
            _setTokens = [];
            _setNegated = false;

            // Peek at the next char to see if it negates this set
            if (charPos + 1 < patternString.Length && patternString[charPos + 1] == '!')
            {
                _setNegated = true;
                charPos += 1;
            }

            return;
        }

        _tokens.Add(new GlobTokenLiteral(ch));
    }

    /// <summary>
    ///     Parses the current character in <paramref name="patternString" />
    ///     assuming the current state is <see cref="GlobParserState.Set" />.
    /// </summary>
    /// <param name="patternString">The full string being parsed.</param>
    /// <param name="charPos">Current character to parse.</param>
    /// <exception cref="Exception">A parsing error occured.</exception>
    private void ParseOneSet(string patternString, ref int charPos)
    {
        char ch = patternString[charPos];

        if (ch == '\\')
        {
            charPos += 1;
            if (charPos >= patternString.Length) throw new Exception("Escape character is not followed by a character");
            char next = patternString[charPos];

            _tokens.Add(new GlobTokenLiteral(next));
            return;
        }

        if (ch == ']')
        {
            FinishSet();
            return;
        }

        // Peek at the next char to see if we need to parse a range
        if (charPos + 1 < patternString.Length && patternString[charPos + 1] == '-')
        {
            charPos += 1;
            (char rangeEnd, bool escaped) = NextRangeChar(patternString, ref charPos);

            // If we get a non-escaped set closure,
            // we can assume the current char and the range char '-' are literals.
            // [a-] = Literal('a'), Literal('-')
            if (rangeEnd == ']' && !escaped)
            {
                _setTokens.Add(new GlobTokenLiteral(ch));
                _setTokens.Add(new GlobTokenLiteral('-'));
                FinishSet();
                return;
            }

            _setTokens.Add(new GlobTokenRange(ch, rangeEnd));
            return;
        }

        _setTokens.Add(new GlobTokenLiteral(ch));
    }

    /// <summary>
    ///     Gets the last character in a range token, and a bool representing whether the character was escaped or not.
    /// </summary>
    /// <param name="patternString">The full string being parsed.</param>
    /// <param name="charPos">Current character being parsed, assumed to be the range character '-'.</param>
    /// <returns>
    ///     A tuple containing the last character in the range, and a bool
    ///     set to true if the last character was escaped by a backslash.
    /// </returns>
    /// <exception cref="Exception">A parsing error occured.</exception>
    private static (char, bool) NextRangeChar(string patternString, ref int charPos)
    {
        charPos += 1;
        if (charPos >= patternString.Length) throw new Exception("Range char is not followed by a character");
        char ch = patternString[charPos];

        if (ch == '\\')
        {
            charPos += 1;
            if (charPos >= patternString.Length) throw new Exception("Escape character is not followed by a character");
            char next = patternString[charPos];
            return (next, true);
        }

        return (ch, false);
    }

    /// <summary>
    ///     Wraps up the parsing of a character set token.
    /// </summary>
    /// <exception cref="Exception">The set is empty.</exception>
    private void FinishSet()
    {
        if (_setTokens.Count == 0) throw new Exception("Set cannot be empty");

        _state = GlobParserState.Root;
        _tokens.Add(new GlobTokenCharacterSet(_setNegated, _setTokens));
    }
}

/// <summary>
///     A state the <see cref="GlobParser" /> can be in.
/// </summary>
internal enum GlobParserState
{
    /// <summary>
    ///     We are currently parsing a single root token.
    /// </summary>
    Root,

    /// <summary>
    ///     We are currently parsing a token in a character set root token.
    /// </summary>
    Set
}
