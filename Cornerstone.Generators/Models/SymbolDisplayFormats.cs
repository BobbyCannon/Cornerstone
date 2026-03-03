#region References

using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public static class SymbolDisplayFormats
{
	#region Constructors

	static SymbolDisplayFormats()
	{
		SymbolName = new SymbolDisplayFormat(
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

		Namespace = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

		TypeDeclaration = new SymbolDisplayFormat(
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
			kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

		FullyQualifiedName = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

		GlobalFullyQualifiedName = new SymbolDisplayFormat(
			SymbolDisplayGlobalNamespaceStyle.Included,
			SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			SymbolDisplayGenericsOptions.IncludeTypeParameters,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

		EventDefinition = new SymbolDisplayFormat(
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
			propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
			memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
			| SymbolDisplayMemberOptions.IncludeExplicitInterface,
			miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
	}

	#endregion

	#region Properties

	public static SymbolDisplayFormat EventDefinition { get; }

	/// <summary>
	/// Cornerstone.Collections.SpeedyTree&lt;Cornerstone.Presentation.MenuItemView&gt;
	/// </summary>
	public static SymbolDisplayFormat FullyQualifiedName { get; }

	/// <summary>
	/// global::System.Collections.Generic.IEnumerable(T);
	/// </summary>
	public static SymbolDisplayFormat GlobalFullyQualifiedName { get; }

	/// <summary>
	/// System.Collections.Generic
	/// </summary>
	public static SymbolDisplayFormat Namespace { get; }

	/// <summary>
	/// "List" or "MethodName"
	/// </summary>
	public static SymbolDisplayFormat SymbolName { get; }

	/// <summary>
	/// class List(T)
	/// </summary>
	public static SymbolDisplayFormat TypeDeclaration { get; }

	#endregion
}