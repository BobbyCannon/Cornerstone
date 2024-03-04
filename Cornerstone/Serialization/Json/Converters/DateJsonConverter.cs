#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Protocols.Osc;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all date types.
/// <see cref="Activator.DateTypes" />
/// </summary>
public class DateJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public DateJsonConverter() : base(Activator.DateTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		consumer.WriteRawString(GetJsonString(value, settings));
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		var sValue = jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonString x => x.Value.ConvertTo(type),
			_ => throw new NotImplementedException()
		};

		return sValue;
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		string sValue;

		switch (value)
		{
			case null:
			{
				sValue = "null";
				break;
			}
			#if !NETSTANDARD
			case DateOnly dateOnly:
			{
				sValue = dateOnly.ToString("O");
				break;
			}
			#endif
			case DateTime dateTime:
			{
				if (dateTime == DateTime.MinValue)
				{
					sValue = "0001-01-01T00:00:00";
					break;
				}

				if (dateTime == DateTime.MaxValue)
				{
					sValue = "9999-12-31T23:59:59.9999999";
					break;
				}

				// "2000-01-02T03:04:12.0000000Z"
				if (dateTime.Millisecond == 0)
				{
					sValue = dateTime.ToUtcDateTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
					break;
				}

				sValue = dateTime.ToUtcDateTime().ToString("O");
				break;
			}
			case DateTimeOffset dateTimeOffset:
			{
				if (dateTimeOffset == DateTimeOffset.MinValue)
				{
					sValue = "0001-01-01T00:00:00+00:00";
					break;
				}
				if (dateTimeOffset == DateTimeOffset.MaxValue)
				{
					sValue = "9999-12-31T23:59:59.9999999+00:00";
					break;
				}

				// "2000-01-02T03:04:12.0000000Z"
				if (dateTimeOffset.Millisecond == 0)
				{
					sValue = dateTimeOffset.ToString("yyyy-MM-ddThh:mm:sszzz");
					break;
				}

				sValue = dateTimeOffset.ToString("O");
				break;
			}
			case IsoDateTime isoDateTime:
			{
				sValue = isoDateTime.ToString();
				break;
			}
			case OscTimeTag oscTime:
			{
				sValue = oscTime.GetOscValueString();
				break;
			}
			default:
			{
				throw new NotSupportedException($"{value.GetType().FullName} is not supported.");
			}
		}

		return "\"" + sValue + "\"";
	}

	#endregion
}