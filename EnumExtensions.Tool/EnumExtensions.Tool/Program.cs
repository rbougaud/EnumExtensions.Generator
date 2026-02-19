using EnumExtensions.Tool.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

Console.WriteLine($"Scanning: {root}");

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

if (modified)
{
    Console.WriteLine("Files updated.");
    Environment.Exit(1); // utile en CI
}
Console.WriteLine("Done.");