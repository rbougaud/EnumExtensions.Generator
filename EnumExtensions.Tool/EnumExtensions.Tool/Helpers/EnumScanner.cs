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
        root = Path.GetFullPath(root);

        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".g.cs"))
            .ToList();

        bool modified = false;
        var attributeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            string code = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var rootNode = await tree.GetRootAsync();

            var enums = rootNode.DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .Where(AttributeExtensions.HasGenerateAttribute)
                .ToList();

            if (enums.Count == 0)
            {
                continue;
            }

            attributeDirs.Add(FindProjectDirectory(Path.GetDirectoryName(file)!, root));

            foreach (var enumDecl in enums)
            {
                if (FileGenerator.Generate(enumDecl, file))
                {
                    modified = true;
                }
            }
        }

        foreach (var dir in attributeDirs)
        {
            AttributeExtensions.EnsureAttributeExists(dir);
        }

        return modified;
    }

    /// <summary>
    /// Remonte depuis le dossier du fichier source jusqu'au premier dossier
    /// contenant un .csproj (sans dépasser <paramref name="root"/>).
    /// À défaut, retourne <paramref name="root"/>.
    /// </summary>
    private static string FindProjectDirectory(string startDirectory, string root)
    {
        var dir = new DirectoryInfo(startDirectory);

        while (dir is not null)
        {
            if (dir.EnumerateFiles("*.csproj").Any())
            {
                return dir.FullName;
            }

            if (string.Equals(
                    Path.TrimEndingDirectorySeparator(dir.FullName),
                    Path.TrimEndingDirectorySeparator(root),
                    StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            dir = dir.Parent!;
        }

        return root;
    }
}
