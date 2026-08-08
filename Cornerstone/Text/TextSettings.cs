#region References

using System;

#endregion

namespace Cornerstone.Text;

public class TextSettings : ITextSettings
{
	#region Constructors

	public TextSettings()
	{
		IndentChar = '\t';
		IndentLength = 1;
		NewLineChars = Environment.NewLine;
	}

	#endregion

	#region Properties

	public char IndentChar { get; set; }
	public uint IndentLength { get; set; }
	public string NewLineChars { get; set; }

	#endregion
}

public interface ITextSettings
{
	#region Properties

	char IndentChar { get; set; }
	uint IndentLength { get; set; }
	string NewLineChars { get; set; }

	#endregion
}