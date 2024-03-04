#region References

using System;
using System.Globalization;
using Cornerstone.Convert;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The converter for all time types.
/// <see cref="Activator.TimeTypes" />
/// </summary>
public class TimeJsonConverter : JsonConverter
{
	#region Constructors

	/// <inheritdoc />
	public TimeJsonConverter() : base(Activator.TimeTypes)
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
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonString x => x.Value.ConvertTo(type),
			_ => throw new NotImplementedException()
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		string sValue;

		switch (value)
		{
			#if (NET6_0_OR_GREATER)
			case TimeOnly timeOnly:
			{
				// "0.03:04:12.0000000Z"
				if (timeOnly.Millisecond == 0)
				{
					sValue = timeOnly.ToString(@"HH\:mm\:ss");
					break;
				}
				sValue = timeOnly.ToString("HH':'mm':'ss.FFFFFFF");
				break;
			}
			#endif
			case TimeSpan timeSpan:
			{
				// "0.03:04:12.0000000Z"
				//if (timeSpan.Milliseconds == 0)
				//{
				//	sValue = timeSpan.ToString(@"d\.hh\:mm\:ss"));
				//	return;
				//}

				//                                  10675199.02:48:05.4775807
				//sValue = timeSpan.ToString("d\\.hh\\:mm\\:ss\\.fffffff"));
				sValue = timeSpan.ToString(null, CultureInfo.InvariantCulture);
				break;
			}
			default:
			{
				throw new NotImplementedException();
			}
		}

		return "\"" + sValue + "\"";
	}

	#endregion
}