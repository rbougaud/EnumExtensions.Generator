using EnumExtensions.Tool;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

Console.WriteLine($"Scanning: {root}");

var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.EndsWith(".g.cs"))
    .ToList();

foreach (var file in files)
{
    var code = await File.ReadAllTextAsync(file);
    var tree = CSharpSyntaxTree.ParseText(code);
    var rootNode = await tree.GetRootAsync();

    var enums = rootNode.DescendantNodes().OfType<EnumDeclarationSyntax>();

    foreach (var enumDecl in enums)
    {
        FileGenerator.Generate(enumDecl, file);
    }
}

Console.WriteLine("Done.");