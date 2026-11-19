using System.Diagnostics;

namespace GlobDemo;

public class GlobMatcher(GlobPattern pattern, string value)
{
    /// <summary>
    ///     Function call memoization table, keyed by (int charPos, int tokenPos)
    /// </summary>
    private readonly Dictionary<(int, int), bool> _memo = new();

    /// <summary>
    ///     Pattern to match <see cref="_value" /> with.
    /// </summary>
    private readonly GlobPattern _pattern = pattern;

    /// <summary>
    ///     String to be matched against <see cref="_pattern" />.
    /// </summary>
    private readonly string _value = value;

    /// <summary>
    ///     Returns whether the glob <paramref name="pattern" /> can match <paramref name="value" />.
    /// </summary>
    /// <param name="pattern">Glob pattern to match <paramref name="value" /> with.</param>
    /// <param name="value">String to be matched against <paramref name="pattern" />.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="pattern" /> can match
    ///     <paramref name="value" />, otherwise <see langword="false" />.
    /// </returns>
    public static bool GlobMatchString(GlobPattern pattern, string value)
    {
        var matcher = new GlobMatcher(pattern, value);
        return matcher.MatchFrom(0, 0);
    }

    /// <summary>
    ///     Returns whether <see cref="_pattern" /> can match <see cref="_value" /> at starting positons
    ///     <paramref name="charPos" /> and <paramref name="tokenPos" />.
    /// </summary>
    /// <remarks>
    ///     This function is memoized. All calls to this function get their results stored in <see cref="_memo" />
    ///     <br /> If the memo table has this function call memorized, this function returns the memorized value.
    /// </remarks>
    /// <param name="charPos">Index of the <see cref="_value" /> character to start matching from.</param>
    /// <param name="tokenPos">Index of the token in <see cref="_pattern" /> to start matching with.</param>
    /// <returns>
    ///     <see langword="true" /> if <see cref="_pattern" /> can match <see cref="_value" />
    ///     starting from the character index <paramref name="charPos" /> and token index
    ///     <paramref name="tokenPos" />, otherwise <see langword="false" />.
    /// </returns>
    private bool MatchFrom(int charPos, int tokenPos)
    {
        // Return the memorized return value for this call if it exists.
        if (_memo.TryGetValue((charPos, tokenPos), out bool memoResult)) return memoResult;

        // Base case: the entire `value` string matches all of `pattern`'s tokens.
        // The entire `value` is matched if there are no more tokens to match against,
        // as well as no more characters to match with a token.
        if (tokenPos == _pattern.Tokens.Count)
        {
            if (charPos == _value.Length)
                // We have matched the entire `value`.
                return _memo[(charPos, tokenPos)] = true;

            // No match was found, there are characters left to match with
            return _memo[(charPos, tokenPos)] = false;
        }

        // There are tokens left to match with, match the rest of the string starting with charPos.
        bool result = _pattern.Tokens[tokenPos] switch
        {
            // AnySequence tokens are special and need their own handler,
            // this is because AnySequence tokens have two different timelines they can evaluate from.
            GlobTokenAnySequence => AnySequenceMatchesFrom(charPos, tokenPos),
            var staticToken => StaticTokenMatchesFrom(staticToken, charPos, tokenPos)
        };

        return _memo[(charPos, tokenPos)] = result;
    }

    /// <summary>
    ///     Returns whether <see cref="_pattern" /> can match <see cref="_value" />  at
    ///     starting positons <paramref name="charPos" /> and <paramref name="tokenPos" />,
    ///     assuming the current token is <see cref="GlobTokenAnySequence" />.
    /// </summary>
    /// <param name="charPos">Index of the <see cref="_value" /> character to start matching from.</param>
    /// <param name="tokenPos">Index of the token in <see cref="_pattern" /> to start matching with.</param>
    /// <returns>
    ///     <see langword="true" /> if <see cref="_pattern" /> can match <see cref="_value" />
    ///     starting from the character index <paramref name="charPos" /> and token index
    ///     <paramref name="tokenPos" />, otherwise <see langword="false" />.
    /// </returns>
    private bool AnySequenceMatchesFrom(int charPos, int tokenPos)
    {
        // AnySequence tokens can be matched from two timelines:
        // A: Match the sequence by stopping the sequence at this character.
        // B: Match the sequence by continuing the sequence for another character.
        // If any of the two timelines produce a total match, this sequence token matches.

        // Attempt timeline A.
        if (MatchFrom(charPos, tokenPos + 1))
            return true;
        // A failed, attempt timeline B.
        if (charPos < _value.Length)
            return MatchFrom(charPos + 1, tokenPos);
        // Timeline A failed, and Timeline B is not possible; there are no more characters to match.
        return false;
    }

    /// <summary>
    ///     Returns whether <see cref="_pattern" /> can match <see cref="_value" />  at
    ///     starting positons <paramref name="charPos" /> and <paramref name="tokenPos" />,
    ///     assuming the current token is <paramref name="staticToken" />.
    /// </summary>
    /// <param name="staticToken">
    ///     The current static token being evaluated. This must not be  <see cref="GlobTokenAnySequence" />.
    /// </param>
    /// <param name="charPos">Index of the <see cref="_value" /> character to start matching from.</param>
    /// <param name="tokenPos">Index of the token in <see cref="_pattern" /> to start matching with.</param>
    /// <returns>
    ///     <see langword="true" /> if <see cref="_pattern" /> can match <see cref="_value" />
    ///     starting from the character index <paramref name="charPos" /> and token index
    ///     <paramref name="tokenPos" />, otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="UnreachableException">
    ///     The <paramref name="staticToken" /> is not a valid <see cref="GlobPatternToken" />.
    /// </exception>
    private bool StaticTokenMatchesFrom(GlobPatternToken staticToken, int charPos, int tokenPos)
    {
        if (charPos >= _value.Length)
            // There are no more characters to match, the pattern's tokens are not yet drained.
            return false;

        char currentChar = _value[charPos];
        bool currentCharMatches = staticToken switch
        {
            GlobTokenLiteral literal => literal.Character == currentChar,
            GlobTokenAnyChar => true,
            GlobTokenAnySequence => throw new Exception(
                "A AnySequence token cannot be matched in a single character context."
            ),
            GlobTokenCharacterSet characterSet => CharacterSetMatches(characterSet, currentChar),
            _ => throw new UnreachableException("All CharacterSetToken variants have cases")
        };

        return
            // The total match depends on this character being a match
            currentCharMatches &&
            // And on the rest of the characters also matching the pattern.
            MatchFrom(charPos + 1, tokenPos + 1);
    }

    /// <summary>
    ///     Match the current character against a <see cref="GlobTokenCharacterSet" />.
    /// </summary>
    /// <param name="characterSet">Set to match against.</param>
    /// <param name="currentChar">Character to match.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="currentChar" /> matches any
    ///     token in the <paramref name="characterSet" />, or if it matches none of
    ///     them and the set is negated; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="UnreachableException">
    ///     A token in the character set includes a non character set token.
    /// </exception>
    private static bool CharacterSetMatches(GlobTokenCharacterSet characterSet, char currentChar)
    {
        // Iterate through this set's tokens for a match.
        foreach (CharacterSetToken characterSetToken in characterSet.Tokens)
        {
            bool tokenMatches = characterSetToken switch
            {
                GlobTokenLiteral literal => literal.Character == currentChar,
                CharacterSetTokenRange range => currentChar >= range.RangeStart && currentChar <= range.RangeEnd,
                _ => throw new UnreachableException("All CharacterSetToken variants have cases")
            };

            // We have a match, we only need one.
            if (tokenMatches)
                // A match normally produces `true`, Negating the result needs to return `false`.
                return !characterSet.Negated;
        }

        // There was no match.
        // Having no match normally produces `false`, negating the result needs to return `true`.
        return characterSet.Negated;
    }
}
