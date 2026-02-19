using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumExtensions.Tool.Helpers;

internal static class FileGenerator
{
    public static bool Generate(EnumDeclarationSyntax enumDecl, string sourceFilePath)
    {
        var enumName = enumDecl.Identifier.Text;

        var namespaceName = enumDecl.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault()?.Name.ToString() ?? "Global";

        var members = enumDecl.Members
            .Select(m => m.Identifier.Text)
            .ToList();

        var code = Template.Generate(enumName, namespaceName, members);

        var projectRoot = Path.GetDirectoryName(sourceFilePath)!;
        var extensionsDir = Path.Combine(projectRoot, "Extensions");
        var output = Path.Combine(extensionsDir, $"{enumName}.Extensions.g.cs");

        if (File.Exists(output))
        {
            var existing = File.ReadAllText(output);
            if (existing == code)
            {
                return false;
            }
        }

        File.WriteAllText(output, code);
        return true;
    }
}
