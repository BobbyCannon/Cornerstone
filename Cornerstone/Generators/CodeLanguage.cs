#region References

using System;

#endregion

namespace Cornerstone.Generators;

/// <summary>
/// The code language. Ex. CSharp, VB, etc
/// </summary>
[Flags]
public enum CodeLanguage
{
	/// <summary>
	/// No format provided
	/// </summary>
	None = 0,

	/// <summary>
	/// CSharp.
	/// </summary>
	CSharp = 0b0000_0001,

	/// <summary>
	/// Visual Basic
	/// </summary>
	VisualBasic = 0b0000_0010,

	/// <summary>
	/// Html
	/// </summary>
	Html = 0b0000_0100,

	/// <summary>
	/// Markdown
	/// </summary>
	Markdown = 0b0000_1000,

	/// <summary>
	/// All Formats
	/// </summary>
	All = CSharp | VisualBasic | Html | Markdown
}