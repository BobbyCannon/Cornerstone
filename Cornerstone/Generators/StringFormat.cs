namespace Cornerstone.Generators;

/// <summary>
/// The string format
/// </summary>
public enum StringFormat
{
	/// <summary>
	/// Verbatim string. ex. C# @"foo\bar"
	/// </summary>
	Verbatim = 0,

	/// <summary>
	/// Regular string. ex C# "foo\\bar"
	/// </summary>
	Regular = 1,

	/// <summary>
	/// Raw string. ex. C# """foo\bar"""
	/// </summary>
	RawString = 2
}