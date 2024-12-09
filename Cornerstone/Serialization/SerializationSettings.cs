#region References

using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Internal;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization;

/// <summary>
/// Settings for a serializer.
/// </summary>
public struct SerializationSettings : ISerializationSettings
{
	#region Properties

	/// <inheritdoc />
	public EnumFormat EnumFormat { get; set; }

	/// <inheritdoc />
	public bool IgnoreDefaultValues { get; set; }

	/// <inheritdoc />
	public bool IgnoreNullValues { get; set; }

	/// <inheritdoc />
	public bool IgnoreReadOnly { get; set; }

	/// <inheritdoc />
	public int MaxDepth { get; set; }

	/// <inheritdoc />
	public NamingConvention NamingConvention { get; set; }

	/// <inheritdoc />
	public TextFormat TextFormat { get; set; }

	/// <inheritdoc />
	public UpdateableAction UpdateableAction { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return HashCodeCalculator.Combine(
			EnumFormat,
			IgnoreDefaultValues,
			IgnoreNullValues,
			IgnoreReadOnly,
			MaxDepth,
			NamingConvention,
			TextFormat,
			UpdateableAction
		);
	}

	#endregion
}

/// <summary>
/// Settings for a serializer.
/// </summary>
public interface ISerializationSettings : IConverterSettings, ITextBuilderSettings
{
	#region Properties

	/// <summary>
	/// Ignore properties that are default values.
	/// </summary>
	bool IgnoreDefaultValues { get; set; }

	/// <summary>
	/// Ignore properties that are null value.
	/// </summary>
	bool IgnoreNullValues { get; set; }

	/// <summary>
	/// Ignore read only properties.
	/// </summary>
	bool IgnoreReadOnly { get; set; }

	/// <summary>
	/// The max depth when serializing.
	/// </summary>
	int MaxDepth { get; set; }

	/// <summary>
	/// The naming convention to use during serialization. Defaults to <see cref="Text.NamingConvention.PascalCase" />.
	/// </summary>
	NamingConvention NamingConvention { get; set; }

	/// <summary>
	/// Optional updatable action to for handling IUpdateable objects.
	/// </summary>
	UpdateableAction UpdateableAction { get; set; }

	#endregion
}