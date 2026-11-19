namespace GlobDemo;

/// <summary>
///     Exception thrown when an error occurs during glob pattern parsing.
/// </summary>
/// <param name="innerException">Inner parsing exception.</param>
[Serializable]
public class GlobParserException(Exception innerException)
    : Exception($"A parsing error occurred: {innerException.Message}", innerException)
{
}

/// <summary>
///     Exception thrown during glob parsing when the parser attempts to read past the end of its input.
/// </summary>
[Serializable]
public class EndOfInputException() : Exception("Attempted to read a character past the pattern string's end");

/// <summary>
///     Exception thrown during glob parsing when an escape character has no following character to escape.
/// </summary>
[Serializable]
public class DanglingEscapeException() : Exception("Escape character has no following character to escape");

/// <summary>
///     Exception thrown during glob parsing when a range character has no following character to end the range with.
/// </summary>
[Serializable]
public class DanglingRangeException() : Exception("Range character has no following range end character");

/// <summary>
///     Exception thrown during glob parsing when closing an empty character set token.
/// </summary>
[Serializable]
public class EmptyCharacterSetException() : Exception("A character set must contain at least one character or range");

/// <summary>
///     Exception thrown during glob parsing when an open character set token is not closed.
/// </summary>
[Serializable]
public class CharacterSetNotClosedException() : Exception("The open character set must be closed with ']'");
