namespace GlobDemo;

/// <summary>
///     Provides sequential access to a string, with the ability to inspect the current and next characters.
/// </summary>
/// <remarks>
///     The cursor initially starts before the first character. Character reads return <see langword="null" />
///     when the requested character position is outside the string.
/// </remarks>
/// <param name="content">The string to read.</param>
public class PeekableString(string content)
{
    /// <summary>
    ///     Current index position of the string.
    /// </summary>
    private int _cursor = -1;

    /// <summary>
    ///     Bool that is true when the current  position is outside the string.
    /// </summary>
    public bool CurrentlyOutOfBounds => _cursor < 0 || _cursor >= content.Length;

    /// <summary>
    ///     Bool that is true when the next position is outside the string.
    /// </summary>
    public bool NextIsOutOfBounds => _cursor + 1 < 0 || _cursor + 1 >= content.Length;

    /// <summary>
    ///     The character at the current position, or <see langword="null" /> if the position is out of bounds.
    /// </summary>
    public char? Current => !CurrentlyOutOfBounds ? content[_cursor] : null;

    /// <summary>
    ///     The character at the next position, or <see langword="null" /> if the next position is out of bounds.
    /// </summary>
    public char? PeekNext => !NextIsOutOfBounds ? content[_cursor + 1] : null;

    /// <summary>
    ///     Advances the cursor to the next position and returns its character.
    /// </summary>
    /// <remarks>
    ///     This function will only ever advance the cursor to the position after the last character. \
    ///     Any call afterward does not update the cursor position.
    /// </remarks>
    /// <returns>
    ///     The character at the new position, or <see langword="null" /> if that position is out of bounds.
    /// </returns>
    public char? ConsumeNext()
    {
        if (_cursor < content.Length)
            _cursor++;

        return _cursor < content.Length ? content[_cursor] : null;
    }
}
