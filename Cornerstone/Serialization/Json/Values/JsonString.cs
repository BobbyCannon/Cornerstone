#region References

using System;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Json;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization.Json.Values;

/// <summary>
/// A class representing a JSON string within the
/// generic JSON object model.
/// </summary>
public class JsonString : JsonValue
{
	#region Constructors

	/// <summary>
	/// Constructs a JSON string value.
	/// </summary>
	public JsonString() : this(string.Empty)
	{
	}

	/// <summary>
	/// Constructs a JSON string value.
	/// </summary>
	/// <param name="value"> The string. This must not be null. </param>
	public JsonString(string value)
	{
		Value = value ?? throw new ArgumentNullException(nameof(value));
	}

	#endregion

	#region Properties

	/// <summary>
	/// The string contents of this JSON string.
	/// </summary>
	public string Value { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override int CompareTo(object obj)
	{
		return obj is JsonString jsonValue
			// todo: add setting for comparison?
			? string.Compare(Value, jsonValue.Value, StringComparison.CurrentCulture)
			: -1;
	}

	/// <summary>
	/// Escapes the given string according to the JSON specification. This will
	/// also add double quotes at start and end. The given string must not be null.
	/// </summary>
	/// <param name="s"> The string to escape. </param>
	/// <returns> A string with the escaped string to. </returns>
	public static string Escape(string s)
	{
		var builder = new TextBuilder();
		Escape(s, builder);
		return builder.ToString();
	}

	/// <summary>
	/// Escapes the given string according to the JSON specification. This will
	/// also add double quotes at start and end. The given string must not be null.
	/// </summary>
	/// <param name="s"> The string to escape. </param>
	/// <param name="builder"> An existing string builder to write the escaped string to. </param>
	public static void Escape(string s, ITextBuilder builder)
	{
		for (var index = 0; index < s.Length; ++index)
		{
			var c = s[index];
			Escape(c, builder);
		}
	}

	/// <summary>
	/// Escapes the given character according to the JSON specification.
	/// </summary>
	/// <param name="character"> The character to escape. </param>
	/// <param name="builder"> An existing string builder to write the escaped string to. </param>
	public static void Escape(char character, ITextBuilder builder)
	{
		switch (character)
		{
			case '"':
			{
				builder.Append("\\\"");
				break;
			}
			case '\\':
			{
				builder.Append("\\\\");
				break;
			}
			case '\b':
			{
				builder.Append("\\b");
				break;
			}
			case '\f':
			{
				builder.Append("\\f");
				break;
			}
			case '\n':
			{
				builder.Append("\\n");
				break;
			}
			case '\r':
			{
				builder.Append("\\r");
				break;
			}
			case '\t':
			{
				builder.Append("\\t");
				break;
			}
			case '/':
			{
				builder.Append('\\');
				builder.Append('/');
				break;
			}
			case '\0':
			{
				builder.Append("\\u0000");
				break;
			}
			default:
			{
				if ((character < 32) || (character > 126))
				{
					builder.Append($"\\u{(int) character:X4}");
					return;
				}

				builder.Append(character);
				break;
			}
		}
	}
	
	/// <summary>
	/// Unescapes the given character according to the JSON specification.
	/// </summary>
	/// <param name="token"> The token to unescape. </param>
	public static string Unescape(JsonTokenData token)
	{
		if (token.Length <= 2)
		{
			return string.Empty;
		}

		var builder = new TextBuilder();
		var escaped = false;
		var quote = token[0];
		var lastOffset = token.Length - 1;

		for (var i = 1; i < lastOffset; i++)
		{
			var c = token[i];

			if (escaped)
			{
				switch (c)
				{
					case '0':
					{
						builder.Append('0');
						break;
					}
					case '\'':
					{
						builder.Append('\'');
						break;
					}
					case '"':
					{
						builder.Append('"');
						break;
					}
					case '\\':
					{
						builder.Append('\\');
						break;
					}
					case '/':
					{
						builder.Append('/');
						break;
					}
					case 'b':
					{
						builder.Append('\b');
						break;
					}
					case 'f':
					{
						builder.Append('\f');
						break;
					}
					case 'n':
					{
						builder.Append("\n");
						break;
					}
					case 'r':
					{
						builder.Append('\r');
						break;
					}
					case 't':
					{
						builder.Append('\t');
						break;
					}
					case 'u':
					{
						var hex1 = token[++i];
						var hex2 = token[++i];
						var hex3 = token[++i];
						var hex4 = token[++i];
						
						// parse the 32 bit hex into an integer codepoint
						var codePoint = Tokenizer.ParseUnicode(hex1, hex2, hex3, hex4);
						builder.Append((char) codePoint);
						break;
					}
					default:
					{
						builder.Append(c);
						break;
					}
				}
				escaped = false;
			}
			else if (c == '\\')
			{
				escaped = true;
			}
			else
			{
				builder.Append(c);
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Escapes the given string according to the JSON specification. This will
	/// also add double quotes at start and end. The given string must not be null.
	/// </summary>
	/// <param name="s"> The string to escape. </param>
	public static string EscapeWithQuotes(string s)
	{
		var builder = new TextBuilder();
		builder.Append('"');
		Escape(s, builder);
		builder.Append('"');
		return builder.ToString();
	}

	/// <summary>
	/// Escapes the given string according to the JSON specification. This will
	/// also add double quotes at start and end. The given string must not be null.
	/// </summary>
	/// <param name="s"> The string to escape. </param>
	/// <param name="builder"> An existing string builder to write the escaped string to. </param>
	public static void EscapeWithQuotes(string s, ITextBuilder builder)
	{
		builder.Append('"');
		Escape(s, builder);
		builder.Append('"');
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Value;
	}

	/// <inheritdoc />
	public override void WriteTo(IObjectConsumer consumer)
	{
		consumer.String(Value);
	}

	#endregion
}