namespace Cornerstone.Text;

/// <summary>
/// The settings for the text writer.
/// </summary>
public struct TextBuilderOptions : ITextBuilderOptions
{
	#region Constructors

	/// <summary>
	/// Default text building settings.
	/// </summary>
	public TextBuilderOptions()
	{
		TextFormat = TextFormat.Indented;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The format for the text output. Defaults to None (flat file).
	/// </summary>
	public TextFormat TextFormat { get; set; }

	#endregion
}

/// <summary>
/// The settings for the text writer.
/// </summary>
public interface ITextBuilderOptions
{
	#region Properties

	/// <summary>
	/// The format for the text output. Defaults to None (flat file).
	/// </summary>
	public TextFormat TextFormat { get; set; }

	#endregion
}