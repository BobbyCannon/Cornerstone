#region References

using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public enum SyntaxKind
{
	None,
	Attribute,
	Comment,
	Error,

	/// <summary>
	/// Keyword (public, void, true, false)
	/// </summary>
	Keyword,
	Method,
	Number,

	/// <summary>
	/// Operator (is, typeof, nameof)
	/// </summary>
	Operator,
	Preprocessor,

	/// <summary>
	/// Statements (switch, case, when, break, return, finally)
	/// </summary>
	Statement,
	String,
	Type,
	Variable
}