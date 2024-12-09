#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Generators;

public class PasswordSettings : Bindable
{
	#region Constructors

	public PasswordSettings()
	{
		MinLength = 16;
		UseSymbols = true;

		UseWords = false;
		UppercaseWords = true;
		UseWordSeparator = false;
		WordSeparator = ' ';
		AppendNumberToWords = true;
	}

	#endregion

	#region Properties

	public bool AppendNumberToWords { get; set; }

	public int MinLength { get; set; }

	public bool UppercaseWords { get; set; }

	public bool UseSymbols { get; set; }

	public bool UseWords { get; set; }

	public bool UseWordSeparator { get; set; }

	public char WordSeparator { get; set; }

	#endregion
}