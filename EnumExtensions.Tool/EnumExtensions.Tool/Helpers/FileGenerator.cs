using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumExtensions.Tool.Helpers;

internal static class FileGenerator
{
    public static bool Generate(EnumDeclarationSyntax enumDecl, string sourceFilePath)
    {
        var enumName = enumDecl.Identifier.Text;

        var namespaceName = GetNamespace(enumDecl);

        var members = enumDecl.Members
            .Select(m => m.Identifier.Text)
            .ToList();

        var code = Template.Generate(enumName, namespaceName, members);

        var projectRoot = Path.GetDirectoryName(sourceFilePath)!;
        var generatedDir = Path.Combine(projectRoot, "Generated", "Enums");

        if (!File.Exists(generatedDir))
        {
            Directory.CreateDirectory(generatedDir);
        }

        var output = Path.Combine(generatedDir, $"{enumName}.Extensions.g.cs");

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

    private static string GetNamespace(SyntaxNode node)
    {
        var namespaces = node.Ancestors()
            .Where(a => a is NamespaceDeclarationSyntax
                     || a is FileScopedNamespaceDeclarationSyntax)
            .Select(a =>
                a switch
                {
                    NamespaceDeclarationSyntax nd => nd.Name.ToString(),
                    FileScopedNamespaceDeclarationSyntax fs => fs.Name.ToString(),
                    _ => null
                })
            .Where(n => n is not null)
            .Reverse()
            .ToList();

        return namespaces.Count > 0
            ? string.Join('.', namespaces)
            : "Global";
    }
}
