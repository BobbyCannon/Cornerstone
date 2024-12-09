#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers.Html;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Parsers.Json;

/// <summary>
/// Represents the data for a JSON token.
/// </summary>
public class JsonTokenData : TokenData<JsonTokenData, JsonTokenType>
{
	#region Properties

	public bool IsPropertyName { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Get the contents of the string. Ex. "Foo\" Bar" => Foo \" Bar
	/// </summary>
	/// <returns> The string in an unescaped format. </returns>
	public string GetStringValue()
	{
		return JsonString.Unescape(this);
	}

	/// <summary>
	/// Update the JsonTokenData with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(JsonTokenData update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			IsPropertyName = update.IsPropertyName;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsPropertyName)), x => x.IsPropertyName = update.IsPropertyName);
		}

		return base.UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			JsonTokenData value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <inheritdoc />
	public override void WriteTo(CodeSyntaxHtmlWriter writer)
	{
		var value = ToString();

		switch (Type)
		{
			case JsonTokenType.Boolean:
			{
				writer.WriteSpan(value, SyntaxColor.Keyword);
				return;
			}
			case JsonTokenType.NumberFloat:
			case JsonTokenType.NumberInteger:
			case JsonTokenType.NumberUnsignedInteger:
			{
				writer.WriteSpan(value, SyntaxColor.Number);
				return;
			}
			case JsonTokenType.String:
			{
				writer.WriteSpan(value, IsPropertyName
					? SyntaxColor.Method
					: SyntaxColor.String
				);
				return;
			}
			default:
			{
				writer.WriteRaw(value);
				return;
			}
		}
	}

	#endregion
}