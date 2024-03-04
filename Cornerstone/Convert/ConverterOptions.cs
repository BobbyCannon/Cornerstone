namespace Cornerstone.Convert;

/// <inheritdoc />
public struct ConverterOptions : IConverterOptions
{
	#region Constructors

	/// <summary>
	/// Options for converting values.
	/// </summary>
	public ConverterOptions()
	{
		EnumFormat = EnumFormat.Name;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public EnumFormat EnumFormat { get; set; }

	#endregion
}

/// <summary>
/// Options for converting type values.
/// </summary>
public interface IConverterOptions
{
	#region Properties

	/// <summary>
	/// The format for the enum options. Defaults to Value.
	/// </summary>
	public EnumFormat EnumFormat { get; set; }

	#endregion
}