# GlobDemo

A small .NET command-line application that checks weather a string matches a glob pattern.

## Requirements

- .NET 11 SDK

## Usage

Run the application with a glob pattern and a value to match.

```sh
dotnet run -- "<pattern>" "<value>"
```

Examples: 

``` sh
dotnet run -- "*.cs" "Program.cs" # Successful match
# TRUE

dotnet run -- "*.cs" "Program.cs.old" # No match found
# FALSE

dotnet run -- "[*.cs" "Program.cs.old" # Invalid pattern
# FALSE (ERROR: A parsing error occurred: The open character set must be closed with ']')
```

## Supported pattern syntax

| Pattern | Meaning |
| --- | --- |
| `?` | Matches exactly one character. |
| `*` | Matches zero or more characters. |
| `[abc]` | Matches one character listed in the set. |
| `[a-z]` | Matches one character in an inclusive range. |
| `[!abc]` | Matches one character not listed in the set. |
| `[!a-z]` | Matches one character not in an inclusive range. |
| `\` | Escapes the next character, treating it literally. |

Examples:

```sh
dotnet run -- "file?.txt" "file1.txt"
# TRUE

dotnet run -- "[a-z]*.txt" "notes.txt"
# TRUE

dotnet run -- "*.md" "notes.txt"
# FALSE
```

Matching is case-sensitive ad applies to the entire input string. \
This application matches raw strings, not filesystem paths: \
No files are inspected, path separators have no special meaning.

## Build

```sh
dotnet build
```
