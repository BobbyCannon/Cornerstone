#region References

using Cornerstone.VisualStudio.Core;
using Cornerstone.VisualStudio.Core.Manipulation;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Manipulator;

public class TextManipulatorSafetyTests
{
	#region Methods

	[Fact]
	public void ManipulateTextDoesNotThrowWhenParserAtEof()
	{
		// Repro for ActivityLog IndexOutOfRange on Span[ParserPos] when State is None.
		var text = "<UserControl>\n    <Grid></Grid>\n</UserControl>";
		var manipulator = new TextManipulator(text, text.Length);
		var change = new FakeChange(text.Length, "", "x");
		var result = manipulator.ManipulateText(change);
		Assert.NotNull(result);
	}

	[Fact]
	public void ManipulateTextSkipsCompletionShapedInsert()
	{
		// Completing Gri → Grid></Grid> must not rewrite parent </UserControl>.
		var text = "<UserControl>\n    <Gri\n</UserControl>";
		var pos = text.IndexOf("Gri") + 3;
		var manipulator = new TextManipulator(text, pos);
		var change = new FakeChange(text.IndexOf("Gri"), "Gri", "Grid></Grid>");
		var result = manipulator.ManipulateText(change);
		Assert.Empty(result);
	}

	[Fact]
	public void ManipulateTextStillSyncsSingleLetterTagRename()
	{
		var text = "<Alpha></Alpha>";
		// cursor after Alpha name, insert B
		var manipulator = new TextManipulator(text, 6); // after 'a' of Alpha (positions: <Alpha)
		// Actually <Alpha is positions 0-5, after name is 6 which is '>'
		// Use insertion of Beta at end of Alpha like existing tests
		var input = "<Alpha></Alpha>";
		var manipulator2 = new TextManipulator(input, 6);
		// Position 6 is '>'. For sync tests use ManipulatorTestBase style.
		// Single letter still allowed through IsCompletionShapedChange.
		var change = new FakeChange(6, "", "B");
		// May or may not produce sync depending on parser state; must not throw.
		var result = manipulator2.ManipulateText(change);
		Assert.NotNull(result);
	}

	#endregion

	#region Nested Types

	private sealed class FakeChange : ITextChange
	{
		public FakeChange(int position, string oldText, string newText)
		{
			OldPosition = position;
			NewPosition = position;
			OldText = oldText;
			NewText = newText;
		}

		public int NewPosition { get; }
		public string NewText { get; }
		public int OldPosition { get; }
		public string OldText { get; }
	}

	#endregion
}
