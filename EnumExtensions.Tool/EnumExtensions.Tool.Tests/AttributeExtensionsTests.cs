using EnumExtensions.Tool.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumExtensions.Tool.Tests;

public class AttributeExtensionsTests
{
    private static EnumDeclarationSyntax ParseEnum(string code)
        => CSharpSyntaxTree.ParseText(code)
            .GetRoot()
            .DescendantNodes()
            .OfType<EnumDeclarationSyntax>()
            .Single();

    [Theory]
    [InlineData("[GenerateEnumExtensions] enum E { A }")]
    [InlineData("[GenerateEnumExtensionsAttribute] enum E { A }")]
    [InlineData("[Obsolete, GenerateEnumExtensions] enum E { A }")]
    public void HasGenerateAttribute_ReturnsTrue_WhenAttributeIsPresent(string code)
    {
        Assert.True(AttributeExtensions.HasGenerateAttribute(ParseEnum(code)));
    }

    [Theory]
    [InlineData("enum E { A }")]
    [InlineData("[Obsolete] enum E { A }")]
    [InlineData("[Flags] enum E { A }")]
    public void HasGenerateAttribute_ReturnsFalse_WhenAttributeIsAbsent(string code)
    {
        Assert.False(AttributeExtensions.HasGenerateAttribute(ParseEnum(code)));
    }
}
