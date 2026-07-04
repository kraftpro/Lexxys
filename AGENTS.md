# Repository Guidelines

## Project Structure & Module Organization
Lexxys is a multi-project .NET solution. Production libraries live under `src/`, with one folder per package, such as `src/Lexxys`, `src/Lexxys.Arguments`, `src/Lexxys.Blob`, and provider packages like `src/Lexxys.Blob.Azure`. Tests and local harnesses live under `test/`; `test/Lexxys.Tests` contains broad coverage, while focused projects such as `test/Lexxys.Arguments.Tests` and `test/Lexxys.Configuration.Tests` cover individual packages. Documentation and grammar/config notes are in `doc/`. Keep generated outputs such as `TestResults/`, Azurite state files, and IDE folders out of commits unless intentionally updating fixtures.

## Build, Test, and Development Commands
Use the .NET SDK selected by the environment; this repository currently builds with .NET 10.

- `dotnet restore Lexxys.sln` restores packages for the full solution.
- `dotnet build Lexxys.sln` builds all solution projects and target frameworks.
- `dotnet test Lexxys.sln` runs all test projects.
- `dotnet test test/Lexxys.Tests/Lexxys.Tests.csproj` runs the main regression suite.
- `dotnet test test/Lexxys.Arguments.Tests/Lexxys.Arguments.Tests.csproj` runs a focused package suite.

Some projects target multiple frameworks (`net10.0`, `net8.0`, `netstandard2.0`, `net462`), so verify changes against the relevant package.

## Coding Style & Naming Conventions
Follow `.editorconfig`: C# files use tabs with width 4, LF line endings, explicit types over `var` except where required by language constraints, file-scoped namespaces, braces, and nullable annotations where enabled. Keep namespaces aligned with folders. Public types and members use PascalCase; private fields and locals follow the surrounding file's existing pattern. Prefer existing helpers in `Lexxys`, `Lexxys.Results`, and related packages before adding new utility abstractions.

## Testing Guidelines
Tests use both MSTest (`test/Lexxys.Tests`) and TUnit in newer focused projects. Match the framework already used by the target test project. Name test files after the feature under test, for example `StringTokenRuleTests.cs` or `ConfigNodeParserTests.cs`, and keep fixtures beside the project that consumes them. Add focused tests for parser, generator, configuration, and storage behavior changes.

## Commit & Pull Request Guidelines
The current history contains terse work-in-progress commits, so prefer clear imperative subjects such as `Fix blob path normalization` or `Add config parser tests`. Pull requests should describe the behavior change, list affected projects, note test commands run, link issues when available, and include screenshots only for web or visual changes under `test/Lexxys.Web`.

## Security & Configuration Tips
Do not commit secrets in `appsettings*.json`, local connection strings, Azurite data, or generated result folders. Keep sample configuration minimal and document required environment variables in the relevant project README or PR description.
