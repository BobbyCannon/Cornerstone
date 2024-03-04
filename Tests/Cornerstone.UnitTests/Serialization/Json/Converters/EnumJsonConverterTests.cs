#region References

using System;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

[TestClass]
public class EnumJsonConverterTests : JsonConverterTest<EnumJsonConverter>
{
	#region Methods

	[TestMethod]
	public void Convert()
	{
		var converter = new EnumJsonConverter();

		// Not Null
		AreEqual(true, converter.CanConvert(typeof(ConsoleColor)));
		AreEqual(ConsoleColor.Blue, converter.ConvertTo(typeof(ConsoleColor), new JsonString("9")));
		AreEqual(ConsoleColor.Blue, converter.ConvertTo(typeof(ConsoleColor), new JsonString("Blue")));

		// Nullable
		AreEqual(true, converter.CanConvert(typeof(ConsoleColor?)));
		AreEqual(ConsoleColor.Blue, converter.ConvertTo(typeof(ConsoleColor?), new JsonString("9")));
		AreEqual(null, converter.ConvertTo(typeof(ConsoleColor?), new JsonNull()));
		AreEqual(null, converter.ConvertTo(typeof(ConsoleColor?), null));
		AreEqual(ConsoleColor.Blue, converter.ConvertTo(typeof(ConsoleColor?), new JsonString("Blue")));
	}

	#endregion
}