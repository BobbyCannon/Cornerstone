#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Serialization.Json.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

[TestClass]
public class PartialUpdateJsonConverterTests : JsonConverterTest<PartialUpdateJsonConverter>
{
	#region Methods

	[TestMethod]
	public void Deserialize()
	{
		var guid = "446EF7B3-F745-44FF-A693-25CD644B45A9";
		var idGuid = new Guid(guid);
		var jsonObject = new JsonObject();
		jsonObject.WriteProperty("Id", new JsonString(guid));
		var expected = new Update { Id = idGuid };
		AreEqual(idGuid, expected.Id);
		var converter = new PartialUpdateJsonConverter();
		var actual = converter.ConvertTo(typeof(Update), jsonObject);

		AreEqual(expected, actual);
		AreEqual("{\"Id\":\"446ef7b3-f745-44ff-a693-25cd644b45a9\"}", expected.ToRawJson());

		var actual2 = Serializer.Instance.FromJson<Update>("{\"Id\":\"446ef7b3-f745-44ff-a693-25cd644b45a9\"}");
		AreEqual(expected, actual2);
	}

	#endregion

	#region Classes

	private class Update : PartialUpdate<Update>
	{
		#region Properties

		public Guid Id
		{
			get => Get<Guid>(nameof(Id));
			set => Set(nameof(Id), value);
		}

		#endregion
	}

	#endregion
}