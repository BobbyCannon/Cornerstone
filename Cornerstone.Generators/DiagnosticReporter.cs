#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators;

public static class DiagnosticReporter
{
	#region Fields

	private static readonly DiagnosticDescriptor _typeIsNotPartial = CreateDescriptor(
		"CSG001",
		"Type is not partial",
		"Type '{0}' must be partial in order for Cornerstone.Generators to generate properties or updateable.");

	private static readonly DiagnosticDescriptor _propertyIsNotPartial = CreateDescriptor(
		"CSG002",
		"Property is not partial",
		"Type '{0}' Property {1} must be partial in order for Cornerstone.Generators to generate the property.");

	#endregion

	#region Properties

	public static SourceProductionContext Context { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(SourceProductionContext spc)
	{
		Context = spc;
	}

	public static void ReportPropertyIsNotPartial(INamedTypeSymbol typeSymbol, IPropertySymbol property)
	{
		CreateDiagnostic(_propertyIsNotPartial, typeSymbol.Locations, typeSymbol.Name, property.Name);
	}

	public static void ReportTypeIsNotPartial(INamedTypeSymbol typeSymbol)
	{
		CreateDiagnostic(_typeIsNotPartial, typeSymbol.Locations, typeSymbol.Name);
	}

	public static void WriteLine(string message)
	{
		Context.ReportDiagnostic(
			Diagnostic.Create(
				new DiagnosticDescriptor(
					id: "CSG000",
					title: "General Message",
					messageFormat: message,
					category: "GeneratorDebug",
					defaultSeverity: DiagnosticSeverity.Info,
					isEnabledByDefault: true),
				Location.None
			));
	}

	private static DiagnosticDescriptor CreateDescriptor(string code, string title, string messageFormat, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
	{
		var tags = severity == DiagnosticSeverity.Error ? new[] { WellKnownDiagnosticTags.NotConfigurable } : Array.Empty<string>();
		return new DiagnosticDescriptor(code, title, messageFormat, "Cornerstone.Generators.Generation", severity, true, customTags: tags);
	}

	private static void CreateDiagnostic(DiagnosticDescriptor descriptor, IEnumerable<Location> locations, params object[] args)
	{
		var locationsList = locations as IReadOnlyList<Location> ?? locations.ToList();
		var diagnostic = locationsList.Count switch
		{
			0 => Diagnostic.Create(descriptor, null, args),
			1 => Diagnostic.Create(descriptor, locationsList[0], args),
			_ => Diagnostic.Create(descriptor, locationsList[0], locationsList.Skip(1), args)
		};
		Context.ReportDiagnostic(diagnostic);
	}

	#endregion
}