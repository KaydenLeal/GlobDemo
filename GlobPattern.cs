namespace GlobDemo;

/// <summary>
///     A tokenized glob matching pattern.
/// </summary>
public class GlobPattern(List<GlobPatternToken> tokens)
{
    /// <summary>
    ///     Flat immutable list containing this pattern's tokens in order.
    /// </summary>
    public readonly IReadOnlyList<GlobPatternToken> Tokens = new List<GlobPatternToken>(tokens).AsReadOnly();
}
