using Microsoft.CodeAnalysis;

namespace EnumExtensions.Tool;

internal static class Helper
{
    internal static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol nestedNs)
            {
                foreach (var type in nestedNs.GetNamespaceTypes())
                    yield return type;
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
            }
        }
    }

    internal static IEnumerable<INamedTypeSymbol> GetEnums(Compilation compilation)
    {
        return compilation.Assembly
            .GlobalNamespace
            .GetNamespaceTypes()
            .Where(t => t.TypeKind == TypeKind.Enum)
            .Where(t => t.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "GenerateEnumExtensionsAttribute"));
    }

}
