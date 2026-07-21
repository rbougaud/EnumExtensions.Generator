# EnumExtensions.Tool

Lightweight, offline enum extension generator for .NET projects.

A **dotnet tool** (not a source generator): it writes plain `.cs` files next to your code, so the generated extensions are committed to your repository and visible like any other source file. No package reference, no build-time magic, no IDE dependency.

## Features

- Attribute-driven: only enums marked `[GenerateEnumExtensions]` are processed
- Strongly-typed, switch-based extensions (no reflection, no boxing)
- Optimized `ToStr`, `ToStringLower`, `ToStringUpper`
- Optimized `Parse` / `TryParse`, with case-insensitive overloads
- `IsDefined`, `GetNames`, `GetValues`, `GetCount` with cached arrays
- Supports nested enums (generates `Container_EnumExtensions`)
- Incremental: files are rewritten only when their content changes
- CI friendly: exits with code `1` when files were created or updated, `0` otherwise
- Zero dependency for the target project: the attribute definition is generated into each project as an `internal` class

## Installation

```bash
dotnet tool install -g EnumExtensions.Tool
# later updates:
dotnet tool update -g EnumExtensions.Tool
```

## Usage

1. Decorate the enums you want extensions for:

```csharp
namespace MyApp.Models;

[GenerateEnumExtensions]
public enum Color { Red, Green, Blue }
```

2. Run the tool at the root of your project or solution:

```bash
enumextensions            # scans the current directory
enumextensions C:\path\to\solution
```

3. Commit the generated files.

On each run the tool:

- generates `GenerateEnumExtensionsAttribute.g.cs` next to every `.csproj` that contains decorated enums (so the attribute always compiles — the very first run resolves the "attribute not found" error shown by the IDE);
- generates `Generated/Enums/<EnumName>Extensions.g.cs` next to each source file containing a decorated enum;
- skips `.g.cs` files while scanning and only rewrites files whose content changed.

## Generated API

```csharp
color.ToStr();                    // switch-based, faster than ToString()
color.ToStringLower();
color.ToStringUpper();
ColorExtensions.Parse("Red");
ColorExtensions.TryParse("red", ignoreCase: true, out var color);
ColorExtensions.IsDefined(color);
ColorExtensions.GetNames();       // cached
ColorExtensions.GetValues();      // cached
ColorExtensions.GetCount();
```

## CI integration

The tool exits with code `1` when it had to create or update files. Run it in CI after checkout to fail the build if someone forgot to regenerate:

```yaml
- run: dotnet tool install -g EnumExtensions.Tool
- run: enumextensions .   # non-zero exit code = generated files are stale
```
