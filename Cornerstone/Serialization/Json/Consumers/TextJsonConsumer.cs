#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;
using Cornerstone.Text.Buffers;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Serialization.Json.Consumers;

/// <summary>
/// JSON text consumer.
/// </summary>
public sealed class TextJsonConsumer : ObjectConsumer<ISerializationOptions>, IObjectConsumer
{
	#region Fields

	private readonly Stack<bool> _arrayOrObjectStack;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public TextJsonConsumer()
		: this(Serializer.DefaultSettings)
	{
	}

	/// <inheritdoc />
	public TextJsonConsumer(ISerializationOptions settings)
		: this(new StringGapBuffer(), settings)
	{
	}

	/// <inheritdoc />
	public TextJsonConsumer(IStringBuffer buffer, ISerializationOptions settings)
		: base(buffer, settings)
	{
		_arrayOrObjectStack = new Stack<bool>();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override IObjectConsumer Boolean(bool value)
	{
		Append(value ? "true" : "false");
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer CompleteObject()
	{
		if (!_arrayOrObjectStack.TryPop(out var isArray))
		{
			//throw new CornerstoneException("Invalid state, this should not be called right now...");
			return this;
		}

		PopIndent();
		Append(isArray ? ']' : '}');
		return this;
	}

	/// <inheritdoc />
	public override ITextBuilder NewLine()
	{
		return Settings.TextFormat == TextFormat.Indented
			? base.NewLine()
			: this;
	}

	/// <inheritdoc />
	public override IObjectConsumer Null()
	{
		Append("null");
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer Number(object value)
	{
		switch (value)
		{
			case byte sValue:
			{
				return Number(sValue);
			}
			case sbyte sValue:
			{
				return Number(sValue);
			}
			case short sValue:
			{
				return Number(sValue);
			}
			case ushort sValue:
			{
				return Number(sValue);
			}
			case int sValue:
			{
				return Number(sValue);
			}
			case uint sValue:
			{
				return Number(sValue);
			}
			case long sValue:
			{
				return Number(sValue);
			}
			case ulong sValue:
			{
				return Number(sValue);
			}
			case decimal sValue:
			{
				return Number(sValue);
			}
			case double sValue:
			{
				return Number(sValue);
			}
			case float sValue:
			{
				return Number(sValue);
			}
			default:
			{
				throw new ArgumentException(Babel.Tower[BabelKeys.ArgumentInvalid], nameof(value));
			}
		}
	}

	/// <summary>
	/// Reset the consumer.
	/// </summary>
	/// <returns> The consumer after reset. </returns>
	public override IObjectConsumer Reset()
	{
		Clear();
		_arrayOrObjectStack.Clear();
		return base.Reset();
	}

	/// <inheritdoc />
	public override IObjectConsumer StartObject(Type type)
	{
		var isArray = ArrayJsonConverter.IsArray(type);
		if (isArray)
		{
			_arrayOrObjectStack.Push(true);
			AppendLineThenPushIndent('[');
			PushConsumerMode(ObjectConsumerMode.Array);
		}
		else
		{
			_arrayOrObjectStack.Push(false);
			AppendLineThenPushIndent('{');
			PushConsumerMode(ObjectConsumerMode.Object);
		}
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer String(string value)
	{
		if (value is null)
		{
			Append("null");
			return this;
		}

		JsonString.EscapeWithQuotes(value, this);
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteObject(object value)
	{
		if (JsonNull.IsNullValue(value))
		{
			Null();
			return this;
		}

		if (AlreadyProcessed(value))
		{
			return this;
		}

		AddReference(value);

		var converter = JsonSerializer.GetConverter(value.GetType());
		converter.Append(value, value.GetType(), this, Settings);

		RemoveReference(value);
		
		return this;
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteProperty(string name, object value)
	{
		WritePropertyName(name);
		WriteObject(value);
		return this;
	}

	/// <summary>
	/// Write the property value to the object consumer.
	/// </summary>
	/// <param name="name"> The property name to write. </param>
	public void WritePropertyName(string name)
	{
		if (Settings.NamingConvention == NamingConvention.CamelCase)
		{
			name = name.ToCamelCase();
		}

		WriteRawString($"\"{name}\"{(Settings.TextFormat != TextFormat.None ? ": " : ":")}");
	}

	/// <inheritdoc />
	public override IObjectConsumer WriteRawString(string value)
	{
		Append(value);
		return this;
	}

	private IObjectConsumer Number(decimal value)
	{
		var text = value.ToString("G", CultureInfo.InvariantCulture);
		Append(text.ContainsAny('.', 'E', 'N', 'I') ? text : text + ".0");
		return this;
	}

	private IObjectConsumer Number(double value)
	{
		if (double.IsNaN(value))
		{
			Append("\"NaN\"");
			return this;
		}
		if (double.IsInfinity(value))
		{
			Append(double.IsNegativeInfinity(value) ? "\"-Infinity\"" : "\"Infinity\"");
			return this;
		}

		var text = value.ToString("R", CultureInfo.InvariantCulture);
		Append(text.ContainsAny('.', 'E', 'N') ? text : text + ".0");
		return this;
	}

	private IObjectConsumer Number(float value)
	{
		if (float.IsNaN(value))
		{
			Append("\"NaN\"");
			return this;
		}
		if (float.IsInfinity(value))
		{
			Append(float.IsNegativeInfinity(value) ? "\"-Infinity\"" : "\"Infinity\"");
			return this;
		}

		var text = value.ToString("R", CultureInfo.InvariantCulture);
		Append(text.ContainsAny('.', 'E', 'N') ? text : text + ".0");
		return this;
	}

	private IObjectConsumer Number(long value)
	{
		Append(value.ToString(CultureInfo.InvariantCulture));
		return this;
	}

	private IObjectConsumer Number(ulong value)
	{
		Append(value.ToString(CultureInfo.InvariantCulture));
		return this;
	}

	#endregion
}