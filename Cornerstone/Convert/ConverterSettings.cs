namespace Cornerstone.Convert;

/// <inheritdoc />
public struct ConverterSettings : IConverterSettings
{
	#region Constructors

	/// <summary>
	/// Options for converting values.
	/// </summary>
	public ConverterSettings()
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
public interface IConverterSettings
{
	#region Properties

	/// <summary>
	/// The format for the enum options. Defaults to Value.
	/// </summary>
	public EnumFormat EnumFormat { get; set; }

	#endregion
}