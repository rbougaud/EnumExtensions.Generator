using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumExtensions.Tool.Helpers;

internal static class EnumScanner
{
    /// <summary>
    /// Scans <paramref name="root"/> for enums and (re)generates their extension classes.
    /// Returns true if at least one file was created or updated.
    /// </summary>
    public static async Task<bool> RunAsync(string root)
    {
        AttributeExtensions.EnsureAttributeExists(root);

        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".g.cs"))
            .ToList();

        bool modified = false;

        foreach (var file in files)
        {
            string code = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var rootNode = await tree.GetRootAsync();

            var enums = rootNode.DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .Where(AttributeExtensions.HasGenerateAttribute);

            foreach (var enumDecl in enums)
            {
                if (FileGenerator.Generate(enumDecl, file))
                {
                    modified = true;
                }
            }
        }

        return modified;
    }
}
