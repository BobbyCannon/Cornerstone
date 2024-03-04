#region References

using System.Globalization;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Parsers.Json;

/// <summary>
/// Represents the data for a JSON token.
/// </summary>
public struct JsonTokenData : ITokenData<JsonTokenType>
{
	#region Properties

	/// <summary>
	/// The boolean value if the current token is a boolean.
	/// </summary>
	public bool BooleanValue { get; set; }

	/// <summary>
	/// The float value if the current token is a float.
	/// </summary>
	public double FloatValue { get; set; }

	/// <summary>
	/// The number value if the current token is a number.
	/// </summary>
	public long IntegerValue { get; set; }

	/// <inheritdoc />
	public int LineNumber { get; set; }

	/// <inheritdoc />
	public int Position { get; set; }

	/// <summary>
	/// The string value if the current token is a string.
	/// </summary>
	public string StringValue { get; set; }

	/// <inheritdoc />
	public JsonTokenType Type { get; set; }

	/// <summary>
	/// The unsigned number value if the current token is an unsigned number.
	/// </summary>
	public ulong UnsignedIntegerValue { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return Type switch
		{
			JsonTokenType.NumberFloat => FloatValue.ToString(CultureInfo.InvariantCulture),
			JsonTokenType.NumberInteger => IntegerValue.ToString(CultureInfo.InvariantCulture),
			JsonTokenType.NumberUnsignedInteger => UnsignedIntegerValue.ToString(CultureInfo.InvariantCulture),
			JsonTokenType.String => JsonString.EscapeWithQuotes(StringValue),
			JsonTokenType.Boolean => BooleanValue ? "true" : "false",
			JsonTokenType.Colon => ":",
			JsonTokenType.Comma => ",",
			JsonTokenType.Null => "null",
			JsonTokenType.CurlyOpen => "{",
			JsonTokenType.CurlyClose => "}",
			JsonTokenType.SquaredOpen => "[",
			JsonTokenType.SquaredClose => "]",
			JsonTokenType.None => "end of stream",
			_ => Type.ToString()
		};
	}

	#endregion
}