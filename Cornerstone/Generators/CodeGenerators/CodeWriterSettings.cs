#region References

using Cornerstone.Convert;
using Cornerstone.Data;
using Cornerstone.Internal;
using Cornerstone.Serialization;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators.CodeGenerators;

/// <inheritdoc />
public struct CodeWriterSettings : ICodeWriterSettings
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
	public CodeWriterMode OutputMode { get; set; }

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
			OutputMode,
			TextFormat,
			UpdateableAction
		);
	}

	#endregion
}

/// <summary>
/// Settings for generating code.
/// </summary>
public interface ICodeWriterSettings : ISerializationSettings
{
	#region Properties

	/// <summary>
	/// The writer mode.
	/// </summary>
	public CodeWriterMode OutputMode { get; set; }

	#endregion
}