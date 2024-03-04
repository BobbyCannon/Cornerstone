#region References

using System;
using System.Data;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

[TestClass]
public class DataTableConverterTests : JsonConverterTest<DataTableConverter>
{
	#region Methods

	[TestMethod]
	public void Convert()
	{
		var table = new DataTable();
		table.Columns.AddRange([
			new DataColumn("Age", typeof(int)),
			new DataColumn("Name", typeof(string)),
			new DataColumn("Birthday", typeof(DateTime))
		]);

		var row = table.NewRow();
		row["Age"] = 21;
		row["Name"] = "John";
		table.Rows.Add(row);

		row = table.NewRow();
		row["Age"] = 22;
		row["Name"] = "Jane";
		row["Birthday"] = new DateTime(1776, 07, 04);
		table.Rows.Add(row);

		var actual = Converter.GetJsonString(table, new SerializationOptions());
		var expected = "[{\"Age\":21,\"Name\":\"John\",\"Birthday\":null},{\"Age\":22,\"Name\":\"Jane\",\"Birthday\":\"1776-07-04T00:00:00Z\"}]";
		AreEqual(expected, actual);

		var jsonObject = JsonSerializer.Parse(actual);
		var actualTable = Converter.ConvertTo(typeof(DataTable), jsonObject);
		AreEqual(table, actualTable);

		actual = Converter.GetJsonString(table, new SerializationOptions { TextFormat = TextFormat.Indented });
		expected = "[\r\n\t{\r\n\t\t\"Age\": 21,\r\n\t\t\"Name\": \"John\",\r\n\t\t\"Birthday\": null\r\n\t},\r\n\t{\r\n\t\t\"Age\": 22,\r\n\t\t\"Name\": \"Jane\",\r\n\t\t\"Birthday\": \"1776-07-04T00:00:00Z\"\r\n\t}\r\n]";

		AreEqual(expected, actual);

		jsonObject = JsonSerializer.Parse(actual);
		actualTable = Converter.ConvertTo(typeof(DataTable), jsonObject);
		AreEqual(table, actualTable);
	}

	#endregion
}