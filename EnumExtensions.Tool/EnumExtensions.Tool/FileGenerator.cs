using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumExtensions.Tool;

internal static class FileGenerator
{
    public static void Generate(EnumDeclarationSyntax enumDecl, string filePath)
    {
        var enumName = enumDecl.Identifier.Text;

        var namespaceName = enumDecl.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault()?.Name.ToString() ?? "Global";

        var members = enumDecl.Members
            .Select(m => m.Identifier.Text)
            .ToList();

        var code = Template.Generate(enumName, namespaceName, members);

        var dir = Path.GetDirectoryName(filePath)!;
        var output = Path.Combine(dir, $"{enumName}.Extensions.g.cs");

        File.WriteAllText(output, code);
    }
}
