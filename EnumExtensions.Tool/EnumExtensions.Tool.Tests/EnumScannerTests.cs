using EnumExtensions.Tool.Helpers;
using Microsoft.CodeAnalysis.CSharp;

namespace EnumExtensions.Tool.Tests;

public sealed class EnumScannerTests : IDisposable
{
    private readonly string _root;

    public EnumScannerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "EnumExtensionsToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string AddSource(string fileName, string code)
    {
        var path = Path.Combine(_root, fileName);
        File.WriteAllText(path, code);
        return path;
    }

    private string GeneratedPath(string className)
        => Path.Combine(_root, "Generated", "Enums", $"{className}.g.cs");

    [Fact]
    public async Task RunAsync_GeneratesExtensionFile_ForEnumWithAttribute()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red, Green }
            """);

        var modified = await EnumScanner.RunAsync(_root);

        Assert.True(modified);
        var generated = GeneratedPath("ColorExtensions");
        Assert.True(File.Exists(generated));

        var content = File.ReadAllText(generated);
        Assert.Contains("public static class ColorExtensions", content);
        Assert.Contains("namespace Demo;", content);
        Assert.Contains("Color.Red", content);
        Assert.Contains("Color.Green", content);
    }

    [Fact]
    public async Task RunAsync_GeneratesExtensionFile_ForFullAttributeName()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensionsAttribute]
            public enum Color { Red }
            """);

        var modified = await EnumScanner.RunAsync(_root);

        Assert.True(modified);
        Assert.True(File.Exists(GeneratedPath("ColorExtensions")));
    }

    [Fact]
    public async Task RunAsync_DoesNotGenerate_ForEnumWithoutAttribute()
    {
        AddSource("Status.cs", """
            namespace Demo;

            public enum Status { On, Off }
            """);

        var modified = await EnumScanner.RunAsync(_root);

        Assert.False(modified);
        Assert.False(File.Exists(GeneratedPath("StatusExtensions")));
    }

    [Fact]
    public async Task RunAsync_DoesNotGenerate_ForEnumWithUnrelatedAttribute()
    {
        AddSource("Status.cs", """
            namespace Demo;

            [System.Obsolete]
            public enum Status { On, Off }
            """);

        var modified = await EnumScanner.RunAsync(_root);

        Assert.False(modified);
        Assert.False(File.Exists(GeneratedPath("StatusExtensions")));
    }

    [Fact]
    public async Task RunAsync_CreatesAttributeDefinitionFile_AtRoot()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red }
            """);

        await EnumScanner.RunAsync(_root);

        var attrFile = Path.Combine(_root, "GenerateEnumExtensionsAttribute.g.cs");
        Assert.True(File.Exists(attrFile));
        Assert.Contains("class GenerateEnumExtensionsAttribute", File.ReadAllText(attrFile));
    }

    [Fact]
    public async Task RunAsync_SecondRun_ReportsNoChanges()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red, Green }
            """);

        var first = await EnumScanner.RunAsync(_root);
        var second = await EnumScanner.RunAsync(_root);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task RunAsync_UpdatesGeneratedFile_WhenEnumChanges()
    {
        var source = AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red, Green }
            """);

        await EnumScanner.RunAsync(_root);

        File.WriteAllText(source, """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red, Green, Blue }
            """);

        var modified = await EnumScanner.RunAsync(_root);

        Assert.True(modified);
        Assert.Contains("Color.Blue", File.ReadAllText(GeneratedPath("ColorExtensions")));
    }

    [Fact]
    public async Task RunAsync_GeneratesPrefixedClass_ForNestedEnum()
    {
        AddSource("Order.cs", """
            namespace Demo;

            public class Order
            {
                [GenerateEnumExtensions]
                public enum State { New, Shipped }
            }
            """);

        var modified = await EnumScanner.RunAsync(_root);

        Assert.True(modified);
        Assert.True(File.Exists(GeneratedPath("Order_StateExtensions")));
    }

    [Fact]
    public async Task RunAsync_GeneratedFile_HasNoSyntaxErrors()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red, Green, Blue }
            """);

        await EnumScanner.RunAsync(_root);

        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(GeneratedPath("ColorExtensions")));
        Assert.Empty(tree.GetDiagnostics());
    }

    [Fact]
    public async Task RunAsync_PlacesAttributeFile_NextToCsproj()
    {
        var projDir = Path.Combine(_root, "MyProj");
        Directory.CreateDirectory(Path.Combine(projDir, "Model"));
        File.WriteAllText(Path.Combine(projDir, "MyProj.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(projDir, "Model", "Color.cs"), """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red }
            """);

        await EnumScanner.RunAsync(_root);

        Assert.True(File.Exists(Path.Combine(projDir, "GenerateEnumExtensionsAttribute.g.cs")));
        Assert.False(File.Exists(Path.Combine(_root, "GenerateEnumExtensionsAttribute.g.cs")));
    }

    [Fact]
    public async Task RunAsync_PlacesAttributeFile_InEachProjectWithAttributedEnums()
    {
        foreach (var name in new[] { "ProjA", "ProjB" })
        {
            var dir = Path.Combine(_root, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(dir, "Color.cs"), $$"""
                namespace {{name}};

                [GenerateEnumExtensions]
                public enum Color { Red }
                """);
        }

        await EnumScanner.RunAsync(_root);

        Assert.True(File.Exists(Path.Combine(_root, "ProjA", "GenerateEnumExtensionsAttribute.g.cs")));
        Assert.True(File.Exists(Path.Combine(_root, "ProjB", "GenerateEnumExtensionsAttribute.g.cs")));
    }

    [Fact]
    public async Task RunAsync_DoesNotPlaceAttributeFile_InProjectWithoutAttributedEnums()
    {
        var projA = Path.Combine(_root, "ProjA");
        var projB = Path.Combine(_root, "ProjB");
        Directory.CreateDirectory(projA);
        Directory.CreateDirectory(projB);
        File.WriteAllText(Path.Combine(projA, "ProjA.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(projB, "ProjB.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(projA, "Color.cs"), """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red }
            """);
        File.WriteAllText(Path.Combine(projB, "Status.cs"), """
            namespace Demo;

            public enum Status { On }
            """);

        await EnumScanner.RunAsync(_root);

        Assert.True(File.Exists(Path.Combine(projA, "GenerateEnumExtensionsAttribute.g.cs")));
        Assert.False(File.Exists(Path.Combine(projB, "GenerateEnumExtensionsAttribute.g.cs")));
    }

    [Fact]
    public async Task RunAsync_DoesNotCreateAttributeFile_WhenNoAttributedEnums()
    {
        AddSource("Status.cs", """
            namespace Demo;

            public enum Status { On, Off }
            """);

        await EnumScanner.RunAsync(_root);

        Assert.False(File.Exists(Path.Combine(_root, "GenerateEnumExtensionsAttribute.g.cs")));
    }

    [Fact]
    public async Task RunAsync_GeneratedAttributeDefinition_IsInternal()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red }
            """);

        await EnumScanner.RunAsync(_root);

        var content = File.ReadAllText(Path.Combine(_root, "GenerateEnumExtensionsAttribute.g.cs"));
        Assert.Contains("internal sealed class GenerateEnumExtensionsAttribute", content);
    }

    [Fact]
    public async Task RunAsync_IgnoresGeneratedFiles_WhenScanning()
    {
        AddSource("Color.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Color { Red }
            """);
        AddSource("Ignored.g.cs", """
            namespace Demo;

            [GenerateEnumExtensions]
            public enum Ignored { A }
            """);

        await EnumScanner.RunAsync(_root);

        Assert.False(File.Exists(GeneratedPath("IgnoredExtensions")));
    }
}
