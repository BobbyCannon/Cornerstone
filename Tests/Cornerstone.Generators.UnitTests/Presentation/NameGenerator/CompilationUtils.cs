#nullable enable
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Generators.Presentation.Common;
using Cornerstone.Generators.Presentation.Common.Domain;
using Cornerstone.Generators.Presentation.Compiler;
using Microsoft.CodeAnalysis;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator;

internal static class CompilationUtils
{
    internal static IEnumerable<ResolvedName> ResolveNames(this IEnumerable<ResolvedXmlName> names, Compilation compilation, XamlXNameResolver nameResolver)
    {
        var compiler = MiniCompiler.CreateRoslyn(new RoslynTypeSystem(compilation), MiniCompiler.PresentationXmlnsDefinitionAttribute);
        return names
            .Select(xmlName =>
            {
                var clrType = compiler.ResolveXamlType(xmlName.XmlType);
                return (clrType, nameResolver.ResolveName(clrType, xmlName.Name, xmlName.FieldModifier));
            })
            .Where(t => t.clrType.IsCornerstoneStyledElement())
            .Select(t => t.Item2);
    }
}
