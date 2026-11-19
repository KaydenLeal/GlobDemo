namespace GlobDemo;

/// <summary>
///     Parses strings into valid <see cref="GlobPattern" />s.
/// </summary>
public class GlobParser(string patternString)
{
    /// <summary>
    ///     The string currently being parsed.
    /// </summary>
    private readonly string _patternString = patternString;

    /// <summary>
    ///     The position of the character being parsed in <see cref="_patternString" />.
    /// </summary>
    private int _charPos;

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
    /// <exception cref="GlobParserException">A parsing error occurred.</exception>
    public static GlobPattern ParsePattern(string patternString)
    {
        var parser = new GlobParser(patternString);

        try
        {
            while (!parser.PatternStringExhausted())
            {
                // Hand the parsing of this character to the proper state handler
                switch (parser._state)
                {
                    case GlobParserState.Root:
                        parser.ParseOneRoot();
                        break;
                    case GlobParserState.Set:
                        parser.ParseOneSet();
                        break;
                }

                parser._charPos++;
            }
        }
        catch (Exception e) when (
            e is EndOfInputException
                or DanglingEscapeException
                or DanglingRangeException
                or EmptyCharacterSetException
        )
        {
            throw new GlobParserException(e);
        }

        return new GlobPattern(parser._tokens);
    }

    /// <summary>
    ///     Parses the current character in <see cref="_patternString" />
    ///     assuming the current state is <see cref="GlobParserState.Root" />.
    /// </summary>
    /// <exception cref="DanglingEscapeException">The current character is a dangling escape character.</exception>
    private void ParseOneRoot()
    {
        switch (CurrentChar())
        {
            case '\\':
            {
                _charPos++;
                if (_charPos >= _patternString.Length)
                    throw new DanglingEscapeException();
                char next = CurrentChar();

                _tokens.Add(new GlobTokenLiteral(next));
                return;
            }
            case '?':
            {
                _tokens.Add(new GlobTokenAnyChar());
                return;
            }
            case '*':
            {
                _tokens.Add(new GlobTokenAnySequence());
                return;
            }
            case '[':
            {
                _state = GlobParserState.Set;
                _setTokens = [];
                _setNegated = false;

                // Peek at the next char to see if it negates this set
                if (PeekChar() == '!')
                {
                    _setNegated = true;
                    _charPos++;
                }

                return;
            }
            case var literal:
            {
                _tokens.Add(new GlobTokenLiteral(literal));
                return;
            }
        }
    }

    /// <summary>
    ///     Parses the current character in <see cref="_patternString" />
    ///     assuming the current state is <see cref="GlobParserState.Set" />.
    /// </summary>
    /// <exception cref="DanglingEscapeException">The current character is a dangling escape character.</exception>
    private void ParseOneSet()
    {
        switch (CurrentChar())
        {
            // 1. Escaped literal
            case '\\':
            {
                _charPos++;
                if (_charPos >= _patternString.Length) throw new DanglingEscapeException();
                char next = CurrentChar();

                _tokens.Add(new GlobTokenLiteral(next));
                return;
            }
            // 2. Set end
            case ']':
                FinishSet();
                return;
            // 3. Range
            case var rangeStart when PeekChar() == '-':
            {
                // Consume the range start character.
                _charPos++;

                // Consume the range end character.
                (char rangeEnd, bool rangeEndEscaped) = GetRangeEndChar();

                // If the range end character is a non-escaped set closure, we can assume
                // the set is ending there, and the range character is not a real range token.
                // The rangeStart character and range '-' character then become literals, and the set is finished.
                if (rangeEnd == ']' && !rangeEndEscaped)
                {
                    _setTokens.Add(new GlobTokenLiteral(rangeStart));
                    _setTokens.Add(new GlobTokenLiteral('-'));
                    FinishSet();
                    return;
                }

                _setTokens.Add(new GlobTokenRange(rangeStart, rangeEnd));
                return;
            }
            // 4. Literal
            case var literal:
            {
                _setTokens.Add(new GlobTokenLiteral(literal));
                return;
            }
        }
    }

    /// <summary>
    ///     Gets the range end character in a <see cref="GlobTokenRange" />, and a bool
    ///     representing whether that character was escaped or not.
    /// </summary>
    /// <returns>
    ///     A tuple containing the range end character for the range, and a bool
    ///     set to true if the range end character was escaped by a backslash.
    /// </returns>
    /// <exception cref="DanglingRangeException">The range character has no following character.</exception>
    /// <exception cref="DanglingEscapeException">The range end character is a dangling escape character.</exception>
    private (char, bool) GetRangeEndChar()
    {
        _charPos++;
        if (_charPos >= _patternString.Length) throw new DanglingRangeException();

        // The character after the range '-' character
        char ch = CurrentChar();

        // Check for an escaped character
        if (ch == '\\')
        {
            _charPos++;
            if (_charPos >= _patternString.Length) throw new DanglingEscapeException();
            char next = CurrentChar();

            // The range end character is an escaped character
            return (next, true);
        }

        // The range end character is a normal character
        return (ch, false);
    }

    /// <summary>
    ///     Wraps up the parsing of a character set token.
    /// </summary>
    /// <exception cref="EmptyCharacterSetException">The set is empty.</exception>
    private void FinishSet()
    {
        if (_setTokens.Count == 0) throw new EmptyCharacterSetException();

        _state = GlobParserState.Root;
        _tokens.Add(new GlobTokenCharacterSet(_setNegated, _setTokens));
    }

    /// <summary>
    ///     Returns the current character in <see cref="_patternString" />.
    /// </summary>
    /// <returns>The character in <see cref="_patternString" /> at index <see cref="_charPos" />.</returns>
    /// <exception cref="EndOfInputException">
    ///     <see cref="_charPos" /> is out of <see cref="_patternString" />'s bounds.
    /// </exception>
    private char CurrentChar()
    {
        return !PatternStringExhausted() ? _patternString[_charPos] : throw new EndOfInputException();
    }

    /// <summary>
    ///     Gets the next char in <see cref="_patternString" /> if its available.
    /// </summary>
    private char? PeekChar()
    {
        if (_charPos + 1 < _patternString.Length) return _patternString[_charPos + 1];
        return null;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if all characters in <see cref="_patternString" /> have been exhausted.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if <see cref="_charPos" /> is out of the range
    ///     of <see cref="_patternString" />, or <see langword="false" /> otherwise.
    /// </returns>
    private bool PatternStringExhausted()
    {
        return _charPos >= _patternString.Length;
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
