namespace GlobDemo;

[Serializable]
public class GlobParserException(Exception innerException)
    : Exception($"A parsing error occurred: {innerException.Message}", innerException)
{
}

[Serializable]
public class EndOfInputException() : Exception("Attempted to read a character past the pattern string's end");

[Serializable]
public class DanglingEscapeException() : Exception("Escape character has no following character to escape");

[Serializable]
public class DanglingRangeException() : Exception("Range character has no following range end character");

[Serializable]
public class EmptyCharacterSetException() : Exception("A character set must contain at least one character or range");
