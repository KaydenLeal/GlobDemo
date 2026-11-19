using System.CommandLine;

namespace GlobDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Setup command line arguments
        RootCommand rootCommand = new("Match a string with a glob pattern.\n" +
                                      "Writes \"TRUE\" to stdout when the string matches successfully, " +
                                      "writes \"FALSE\" otherwise");

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
            try
            {
                GlobPattern pattern = GlobParser.ParsePattern(parseResult.GetRequiredValue(patternArgument));
                string value = parseResult.GetRequiredValue(valueArgument);

                Console.WriteLine(GlobMatcher.GlobMatchString(pattern, value) ? "TRUE" : "FALSE");
            }
            catch (GlobParserException e)
            {
                Console.Error.WriteLine($"FALSE (ERROR: {e.Message})");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"FALSE (ERROR: An unknown error occurred: {e.Message})");
            }
        });

        // Parse the command line arguments.
        rootCommand.Parse(args).Invoke();
    }
}
