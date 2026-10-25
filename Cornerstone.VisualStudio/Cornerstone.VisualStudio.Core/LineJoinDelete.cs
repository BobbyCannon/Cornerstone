namespace Cornerstone.VisualStudio.Core;

/// <summary>
/// Delete at the end of a line joins the next line.
/// The line break and that line's leading spaces or tabs become one space.
/// </summary>
public static class LineJoinDelete
{
	#region Constants

	public const string Replacement = " ";

	#endregion

	#region Methods

	/// <summary>
	/// Length of the line break plus the next line's indent, when that span should
	/// be replaced with <see cref="Replacement"/>. False when Delete should stay with the editor.
	/// </summary>
	public static bool TryGetReplaceLength(int lineBreakLength, string nextLine, out int replaceLength)
	{
		replaceLength = 0;
		if (lineBreakLength <= 0)
		{
			return false;
		}

		var indent = IndentLength(nextLine);
		if (indent == 0)
		{
			return false;
		}

		replaceLength = lineBreakLength + indent;
		return true;
	}

	private static int IndentLength(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return 0;
		}

		var i = 0;
		while ((i < line.Length) && ((line[i] == ' ') || (line[i] == '\t')))
		{
			i++;
		}

		return i;
	}

	#endregion
}
