namespace GlobDemo;

/// <summary>
///     A tokenized glob matching pattern.
/// </summary>
public class GlobPattern(List<GlobPatternToken> tokens)
{
    /// <summary>
    ///     Flat list containing this pattern's tokens in order.
    /// </summary>
    public List<GlobPatternToken> Tokens = tokens;
}
