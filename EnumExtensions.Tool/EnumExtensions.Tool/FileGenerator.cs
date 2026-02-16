using Microsoft.CodeAnalysis;

namespace EnumExtensions.Tool;

internal static class FileGenerator
{
    internal static void Generate(INamedTypeSymbol enumSymbol)
    {
        var name = enumSymbol.Name;
        var ns = enumSymbol.ContainingNamespace.ToDisplayString();

        var members = enumSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.ConstantValue is not null)
            .Select(f => f.Name)
            .ToList();

        var source = Template.Generate(name, ns, members);

        var enumFile = enumSymbol.Locations[0].SourceTree!.FilePath;
        var folder = Path.GetDirectoryName(enumFile)!;

        var outputPath = Path.Combine(folder, $"{name}Extensions.g.cs");

        File.WriteAllText(outputPath, source);

        Console.WriteLine($"Generated {name}Extensions.g.cs");
    }

}
