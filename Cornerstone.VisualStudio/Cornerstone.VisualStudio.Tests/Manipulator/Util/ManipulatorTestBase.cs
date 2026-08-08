#region References

using System.Collections.Generic;
using Cornerstone.VisualStudio.Core;
using Cornerstone.VisualStudio.Core.Manipulation;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Manipulator.Util;

public class ManipulatorTestBase
{
	#region Methods

	/// <summary>
	/// Asserts that after user writes text it will be replaced
	/// </summary>
	/// <param name="baseText"> Text, $ tag marks cursor </param>
	/// <param name="userInput"> Text to input at cursor ($) </param>
	/// <param name="expectedOutput"> Final text with replacements </param>
	public void AssertInsertion(string baseText, string userInput, string expectedOutput)
	{
		var (cursor, text) = TestUtils.PrepareTextWithCursor(baseText);
		var inputText = baseText.Replace("$", userInput);

		var manipulator = new TextManipulator(inputText, cursor);

		var change = new TextChange(cursor, userInput, cursor, "");
		var manipulations = manipulator.ManipulateText(change);
		var actualOutput = ApplyManipulations(inputText, manipulations);

		Assert.Equal(expectedOutput, actualOutput);
	}

	/// <summary>
	/// Asserts that after user writes text it will be replaced
	/// </summary>
	/// <param name="baseText"> Text, $ tag marks cursor </param>
	/// <param name="userInput"> Text to input at cursor ($) </param>
	/// <param name="expectedOutput"> Final text with replacements </param>
	public void AssertReplacement(string baseText, string userInput, string expectedOutput)
	{
		var (cursor, inputText, span) = TestUtils.PrepareTextWithSpan(baseText, userInput);

		var manipulator = new TextManipulator(inputText, cursor);

		var change = new TextChange(cursor, userInput, cursor, span);
		var manipulations = manipulator.ManipulateText(change);
		var actualOutput = ApplyManipulations(inputText, manipulations);

		Assert.Equal(expectedOutput, actualOutput);
	}

	private string ApplyManipulations(string text, IList<TextManipulation> manipulations)
	{
		foreach (var manipulation in manipulations)
		{
			if (manipulation.Type == ManipulationType.Insert)
			{
				text = text.Insert(manipulation.Start, manipulation.Text);
			}
			if (manipulation.Type == ManipulationType.Delete)
			{
				text = text.Remove(manipulation.Start, manipulation.End - manipulation.Start);
			}
		}
		return text;
	}

	#endregion
}

public class TextChange : ITextChange
{
	#region Constructors

	public TextChange(int newPosition, string newText, int oldPosition, string oldText)
	{
		(NewPosition, NewText, OldPosition, OldText) = (newPosition, newText, oldPosition, oldText);
	}

	#endregion

	#region Properties

	public int NewPosition { get; set; }

	public string NewText { get; set; }

	public int OldPosition { get; set; }

	public string OldText { get; set; }

	#endregion

	#region Methods

	public static TextChange Insertion(int position, string text)
	{
		return new TextChange(position, text, position, "");
	}

	public static TextChange Replacement(int position, string newText, string oldText)
	{
		return new TextChange(position, newText, position, oldText);
	}

	#endregion
}