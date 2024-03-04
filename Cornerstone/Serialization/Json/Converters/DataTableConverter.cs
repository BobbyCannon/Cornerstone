#region References

using System;
using System.Data;
using System.Linq;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Consumer;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

/// <summary>
/// The DataTable converter.
/// </summary>
public class DataTableConverter : JsonConverter
{
	#region Methods

	/// <inheritdoc />
	public override void Append(object value, Type valueType, IObjectConsumer consumer, ISerializationOptions settings)
	{
		if (value == null)
		{
			consumer.Null();
			return;
		}

		if (value is DataTable dValue)
		{
			var writer = consumer.StartObject(typeof(JsonArray));
			WriteDataTable(writer, dValue, settings);
			consumer.CompleteObject();
			return;
		}

		throw new NotSupportedException("This value is not supported by this converter.");
	}

	/// <inheritdoc />
	public override bool CanConvert(Type type)
	{
		return (type == typeof(DataTable))
			|| type.ImplementsType(typeof(DataTable));
	}

	/// <inheritdoc />
	public override object ConvertTo(Type type, JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonNull => Converter.ConvertTo(null, type),
			JsonArray x => x.ConvertTo(type),
			JsonObject x => x.ConvertTo(type),
			_ => throw new NotImplementedException()
		};
	}

	/// <inheritdoc />
	public override string GetJsonString(object value, ISerializationOptions settings)
	{
		var consumer = new TextJsonConsumer(settings);
		Append(value, value?.GetType(), consumer, settings);
		return consumer.ToString();
	}

	/// <summary>
	/// Convert the data table to JSON.
	/// </summary>
	/// <param name="dataTable"> The data table to serialize. </param>
	/// <param name="settings"> The serializer settings. </param>
	/// <returns> The data table in JSON format. </returns>
	public static string ToJson(DataTable dataTable, ISerializationOptions settings)
	{
		var consumer = new TextJsonConsumer(settings);
		WriteDataTable(consumer, dataTable, settings);
		return consumer.ToString();
	}

	/// <summary>
	/// Write the data table to a serializer consumer.
	/// </summary>
	/// <param name="consumer"> The consumer to write to. </param>
	/// <param name="dataTable"> The data table to write. </param>
	/// <param name="settings"> </param>
	public static void WriteDataTable(IObjectConsumer consumer, DataTable dataTable, ISerializationOptions settings)
	{
		var firstRow = true;
		var textBuilder = consumer as ITextBuilder;

		foreach (DataRow row in dataTable.Rows)
		{
			if (!firstRow)
			{
				consumer.WriteRawString(",");
				textBuilder?.NewLine();
			}

			var writer = consumer.StartObject(typeof(JsonObject));
			var firstProperty = true;

			foreach (DataColumn column in row.Table.Columns)
			{
				if (!firstProperty)
				{
					consumer.WriteRawString(",");
					textBuilder?.NewLine();
				}

				var columnValue = row[column];
				writer.WriteProperty(column.ColumnName, columnValue);
				firstProperty = false;
			}

			textBuilder?.NewLine();
			writer.CompleteObject();
			firstRow = false;
		}

		textBuilder?.NewLine();
	}

	internal static bool ParseAsRow(DataTable table, out DataRow value, JsonObject jValue)
	{
		if (jValue == null)
		{
			value = null;
			return false;
		}

		var row = table.NewRow();

		foreach (var key in jValue.Keys)
		{
			var column = table.Columns[key];
			if (column == null)
			{
				var columnType = GetBestType(jValue[key]);
				column = table.Columns.Add(key, columnType);
			}

			row[key] = jValue[key].ConvertTo(column.DataType);
		}

		value = row;
		return true;
	}

	internal static bool ParseAsTable(Type toType, out object value, JsonArray jValue)
	{
		if (toType != typeof(DataTable))
		{
			value = null;
			return false;
		}

		var table = (DataTable) toType.CreateInstance();

		for (var index = 0; index < jValue.Count; index++)
		{
			var item = jValue[index] as JsonObject;
			if (item == null)
			{
				continue;
			}

			if (ParseAsRow(table, out var row, item))
			{
				row.EndEdit();
				table.Rows.Add(row);
			}
		}

		value = table;
		return true;
	}

	private static Type GetBestType(JsonValue jsonValue)
	{
		return jsonValue switch
		{
			JsonBoolean => typeof(bool),
			JsonNumber sValue => sValue.Value.GetType(),
			JsonString => typeof(string),
			_ => typeof(object)
		};
	}

	#endregion
}