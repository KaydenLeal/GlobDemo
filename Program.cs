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

        // Define main program functionality to execute on argument parsing success.
        rootCommand.SetAction(parseResult =>
        {
            GlobPattern pattern = GlobParser.ParsePattern(parseResult.GetRequiredValue(patternArgument));
            string value = parseResult.GetRequiredValue(valueArgument);

            Console.WriteLine(GlobMatcher.GlobMatchString(pattern, value));
        });

        // Parse the command line arguments.
        rootCommand.Parse(args).Invoke();
    }
}
