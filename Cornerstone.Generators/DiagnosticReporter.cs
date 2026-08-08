#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators;

/// <summary>
/// Reports generator diagnostics. Context is scoped per source-output execution via
/// <see cref="AsyncLocal{T}"/> so parallel project builds in the same compiler process
/// do not clobber each other's <see cref="SourceProductionContext"/>.
/// </summary>
public static class DiagnosticReporter
{
	#region Fields

	private static readonly AsyncLocal<SourceProductionContext?> _context = new();

	private static readonly DiagnosticDescriptor _typeIsNotPartial = CreateDescriptor(
		"CSG001",
		"Type is not partial",
		"Type '{0}' must be partial in order for Cornerstone.Generators to generate properties or updateable.",
		DiagnosticSeverity.Error
	);

	private static readonly DiagnosticDescriptor _typeMissingTestClassAttribute = CreateDescriptor(
		"CSG003",
		"Type missing TestClass attribute",
		"Type '{0}' is missing the [TestClass] attribute.",
		DiagnosticSeverity.Error
	);

	private static readonly DiagnosticDescriptor _propertyIsNotPartial = CreateDescriptor(
		"CSG002",
		"Property is not partial",
		"Type '{0}.{1}' Property must be partial in order for Cornerstone.Generators to generate the property.",
		DiagnosticSeverity.Error
	);

	#endregion

	#region Methods

	/// <summary>
	/// Binds diagnostics to <paramref name="spc"/> for the current async/execution flow.
	/// Dispose at the end of the source-output callback.
	/// </summary>
	public static IDisposable BeginScope(SourceProductionContext spc)
	{
		var previous = _context.Value;
		_context.Value = spc;
		return new Scope(previous);
	}

	/// <summary>
	/// Prefer <see cref="BeginScope"/> so concurrent generator runs stay isolated.
	/// </summary>
	public static void Initialize(SourceProductionContext spc)
	{
		_context.Value = spc;
	}

	public static void ReportMissingTestClassAttribute(INamedTypeSymbol typeSymbol)
	{
		CreateDiagnostic(_typeMissingTestClassAttribute, typeSymbol.Locations, typeSymbol.Name);
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
		var context = _context.Value;
		if (context == null)
		{
			return;
		}

		context.Value.ReportDiagnostic(
			Diagnostic.Create(
				new DiagnosticDescriptor(
					"CSG000",
					"General Message",
					message,
					"GeneratorDebug",
					DiagnosticSeverity.Info,
					true),
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
		var context = _context.Value;
		if (context == null)
		{
			return;
		}

		var locationsList = locations as IReadOnlyList<Location> ?? locations.ToList();
		var diagnostic = locationsList.Count switch
		{
			0 => Diagnostic.Create(descriptor, null, args),
			1 => Diagnostic.Create(descriptor, locationsList[0], args),
			_ => Diagnostic.Create(descriptor, locationsList[0], locationsList.Skip(1), args)
		};
		context.Value.ReportDiagnostic(diagnostic);
	}

	#endregion

	#region Classes

	private sealed class Scope : IDisposable
	{
		private readonly SourceProductionContext? _previous;
		private bool _disposed;

		public Scope(SourceProductionContext? previous)
		{
			_previous = previous;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_context.Value = _previous;
		}
	}

	#endregion
}
