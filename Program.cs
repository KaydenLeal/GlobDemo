using System.CommandLine;

namespace GlobDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Setup command line arguments
        RootCommand rootCommand = new("Match a string with a glob pattern");

        Argument<string> patternArgument = new("pattern")
        {
            Description = "Glob pattern to match with"
        };
        rootCommand.Arguments.Add(patternArgument);

        Argument<string> valueArgument = new("value")
        {
            Description = "Value to match with the glob pattern"
        };
        rootCommand.Arguments.Add(valueArgument);

        rootCommand.SetAction(parseResult =>
        {
            string pattern = parseResult.GetRequiredValue(patternArgument);
            string value = parseResult.GetRequiredValue(valueArgument);

            Run(pattern, value);
        });

        rootCommand.Parse(args).Invoke();
    }

    private static void Run(string patternString, string value)
    {
        GlobPattern pattern = GlobParser.ParsePattern(patternString);
        Console.WriteLine(pattern.Tokens.Count);

        Console.WriteLine(patternString);
        Console.WriteLine(value);
    }
}
