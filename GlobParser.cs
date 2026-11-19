namespace GlobDemo;

/// <summary>
///     Parses strings into valid <see cref="GlobPattern" />s.
/// </summary>
public class GlobParser(string patternString)
{
    /// <summary>
    ///     The string currently being parsed.
    /// </summary>
    private readonly PeekableString _patternString = new(patternString);

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
            while (parser._patternString.ConsumeNext() != null)
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
    /// <exception cref="EndOfInputException"><see cref="_patternString" /> has reached its end.</exception>
    private void ParseOneRoot()
    {
        switch (_patternString.Current)
        {
            case '\\':
            {
                char escaped = _patternString.ConsumeNext() ?? throw new DanglingEscapeException();
                _tokens.Add(new GlobTokenLiteral(escaped));
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
                if (_patternString.PeekNext == '!')
                {
                    _patternString.ConsumeNext();
                    _setNegated = true;
                }

                return;
            }
            case var literal:
            {
                if (literal is null) throw new EndOfInputException();
                _tokens.Add(new GlobTokenLiteral(literal.Value));
                return;
            }
        }
    }

    /// <summary>
    ///     Parses the current character in <see cref="_patternString" />
    ///     assuming the current state is <see cref="GlobParserState.Set" />.
    /// </summary>
    /// <exception cref="DanglingEscapeException">The current character is a dangling escape character.</exception>
    /// <exception cref="EndOfInputException"><see cref="_patternString" /> has reached its end.</exception>
    private void ParseOneSet()
    {
        switch (_patternString.Current)
        {
            // 1. Escaped literal
            case '\\':
            {
                char next = _patternString.ConsumeNext() ?? throw new DanglingEscapeException();
                _tokens.Add(new GlobTokenLiteral(next));
                return;
            }
            // 2. Set end
            case ']':
                FinishSet();
                return;
            // 3. Range
            case var rangeStart when _patternString.PeekNext == '-':
            {
                if (rangeStart is null) throw new EndOfInputException();

                // Consume the range start character.
                _patternString.ConsumeNext();

                // Consume the range end character.
                (char rangeEnd, bool rangeEndEscaped) = GetRangeEndChar();

                // If the range end character is a non-escaped set closure, we can assume
                // the set is ending there, and the range character is not a real range token.
                // The rangeStart character and range '-' character then become literals, and the set is finished.
                if (rangeEnd == ']' && !rangeEndEscaped)
                {
                    _setTokens.Add(new GlobTokenLiteral(rangeStart.Value));
                    _setTokens.Add(new GlobTokenLiteral('-'));
                    FinishSet();
                    return;
                }

                _setTokens.Add(new GlobTokenRange(rangeStart.Value, rangeEnd));
                return;
            }
            // 4. Literal
            case var literal:
            {
                if (literal is null) throw new EndOfInputException();

                _setTokens.Add(new GlobTokenLiteral(literal.Value));
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
        char ch = _patternString.ConsumeNext() ?? throw new DanglingRangeException();

        // Check for an escaped character
        if (ch == '\\')
        {
            char next = _patternString.ConsumeNext() ?? throw new DanglingEscapeException();

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
