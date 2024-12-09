#region References

using Cornerstone.Convert;

#endregion

namespace Cornerstone.Parsers.Csv;

/// <summary>
/// Options for the CSV parser.
/// </summary>
public struct CsvConverterSettings : IConverterSettings
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the CSV options.
	/// </summary>
	public CsvConverterSettings()
	{
		EnumFormat = EnumFormat.Value;
		Delimiter = ',';
		FieldLimit = 0;
		HasHeader = false;
		TrimFields = false;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The character delimiter for the CSV file.
	/// </summary>
	public char Delimiter { get; set; }

	/// <inheritdoc />
	public EnumFormat EnumFormat { get; set; }

	/// <summary>
	/// Optional limit to not allow fields to be larger than value set. If zero or less, then fields will not be limited.
	/// </summary>
	public int FieldLimit { get; set; }

	/// <summary>
	/// An options to parse out a header from the CSV file.
	/// </summary>
	public bool HasHeader { get; set; }

	/// <summary>
	/// If true start/end spaces are excluded from field values (except values in quotes). True by default.
	/// </summary>
	public bool TrimFields { get; set; }

	#endregion
}