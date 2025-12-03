# Lexxys.Arguments

Small command-line parser for Lexxys projects. It can parse arguments dynamically with `Arguments` or bind them to typed option models with `Arguments.Parse<T>()`, `CliOption`, `CliCommand`, and `CliParameters`.

## Example

```csharp
using Lexxys;

[CliParameters(IgnoreCase = true)]
public partial class Options
{
    [CliOption("i", Required = true)]
    public string? InputFile { get; init; }	// parameter name '--input-file', actepts '-i', '-in-fi', or '-infi' (when Abc) etc.

    public bool Verbose { get; init; } // porameter '--verbose', eccepts '-v', '-verb' ...

    [CliCommand()]
    public RunOptions? Run { get; init; } // command 'run' or 'r', 'ru'
}

public partial class RunOptions
{
    [CliOption("x")]
    public RunMode Mode { get; init; } // parameter '--mode' or '-x', '-m', ...
}

public enum RunMode
{
    A,
    B,
    C
}

var args = Arguments.Parse<Options>(args);
if (args.HasErrors)
{
    Console.WriteLine(String.Join(Environment.NewLine, args.Errors));
    args.Usage();
}
```

## Syntax Rules

- Options use `-name` or `--name`; `/name` is accepted when `AllowSlash` is enabled.
- Option values can be written as `--name=value`, `--name:value`, or `--name value` when the corresponding separators are enabled.
- Boolean options are switches: `--verbose` means `true`.
- Short aliases can be used with one dash, for example `-v` or `-m 2`.
- Single-character options can be combined, for example `-abc`, when `CombineOptions` is enabled.
- `--` stops option parsing and starts positional arguments when `DoubleDashSeparator` is enabled.
- Positional members are declared with `[CliOption(Positional = true)]`.
- Arrays and common list types accept comma-separated values, for example `--tag=a,b,c`.
- Commands are plain tokens that select a nested command model; following options are parsed in that command.
- Help is requested with `-?`, `-h`, or `--help`.
- Member names are converted to CLI names by default: `InputFile` becomes `input-file`.
- Matching is case-sensitive by default; enable `IgnoreCase` to ignore case.
- Name separators `-`, `_`, and `.` can be ignored during matching with `IgnoreNameSeparators`.

## Parameter Matching

- A named option matches its `Name` first, then any values in `Alias`.
- If `Name` is omitted, the member name is converted to a CLI name, for example `OutputPath` becomes `output-path`.
- Matching normally compares the full option name, including separators.
- With `IgnoreNameSeparators`, `input-file`, `input_file`, `input.file`, and `inputfile` are treated as the same name.
- With `IgnoreCase`, option and command names are matched case-insensitively.
- A short token such as `-v` can match a one-character option name or alias.
- A long token such as `--verbose` can match any option name or alias.
- When `StrictDoubleDash` is enabled, multi-character options must use `--`.
- Command options are matched only inside the selected command.
- Positional values match positional parameters in declaration order.

## Notes

- Use `ArgumentsBuilder` when the command shape is built manually at runtime.
- Use `[CliParameters]` on `partial` models to enable generated builder/parser support.
- Use `AllowUnknown` only when unknown options should be collected instead of reported as errors.
