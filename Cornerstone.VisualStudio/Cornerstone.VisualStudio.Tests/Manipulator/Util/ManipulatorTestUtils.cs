#region References

using System.Linq;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Manipulator.Util;

internal class TestUtils
{
	#region Methods

	/// <summary>
	/// Parses string with cursor marked as $ to input string and cursor position
	/// </summary>
	/// <param name="baseText"> </param>
	/// <returns> </returns>
	public static (int cursor, string text) PrepareTextWithCursor(string baseText)
	{
		var cursors = baseText.Where(n => n.Equals('$')).Count();
		var thereIsSingleCursor = cursors == 1;
		Assert.True(thereIsSingleCursor);

		var cursor = baseText.IndexOf("$");
		var text = baseText.Replace("$", "");

		return (cursor, text);
	}

	/// <summary>
	/// Parses string with span of text between $$
	/// </summary>
	/// <param name="baseText"> Text withc two $ characters </param>
	/// <returns> Span start, text and span </returns>
	public static (int cursor, string text, string span) PrepareTextWithSpan(string baseText, string userInput = "")
	{
		var cursors = baseText.Where(n => n.Equals('$')).Count();
		var thereIsSingleSpan = cursors == 2;
		Assert.True(thereIsSingleSpan);

		var spanStart = baseText.IndexOf("$");
		var spanEnd = baseText.LastIndexOf("$");
		var cursor = spanStart;

		var start = baseText.Substring(0, spanStart);
		var end = baseText.Substring(spanEnd + 1);
		var text = start + userInput + end;

		var span = baseText.Substring(spanStart + 1, spanEnd - 1 - spanStart);

		return (cursor, text, span);
	}

	#endregion
}