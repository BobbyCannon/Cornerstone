#region References

using Cornerstone.Collections;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

[TestClass]
public class ArrayJsonConverterTests : JsonConverterTest<ArrayJsonConverter>
{
	#region Methods

	[TestMethod]
	public void Convert()
	{
		var collection = new IntCollection { 1, 2, 3, 4, 5 };
		ProcessConverter(collection, "[1,2,3,4,5]", "[\r\n\t1,\r\n\t2,\r\n\t3,\r\n\t4,\r\n\t5\r\n]");
	}

	[TestMethod]
	public void ConvertTo()
	{
		var actual = Converter.ConvertTo(typeof(string[]), new JsonArray());
		var type = actual.GetType();
		AreEqual(typeof(string[]), type);
	}

	#endregion

	#region Classes

	public class IntCollection : SpeedyList<int>
	{
	}

	#endregion
}