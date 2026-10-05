using System;
using XamlX.TypeSystem;

namespace Cornerstone.Generators.Presentation.Common;

internal static class ResolverExtensions
{
    public static bool IsCornerstoneStyledElement(this IXamlType clrType) =>
        Inherits(clrType, "Cornerstone.Presentation.StyledElement");
    public static bool IsWindow(this IXamlType clrType) =>
        Inherits(clrType, "Cornerstone.Presentation.Controls.Window");

    private static bool Inherits(IXamlType clrType, string metadataName)
    {
        if (string.Equals(clrType.FullName, metadataName, StringComparison.Ordinal))
            return true;
        return clrType.BaseType is { } baseType && Inherits(baseType, metadataName);
    }
}
