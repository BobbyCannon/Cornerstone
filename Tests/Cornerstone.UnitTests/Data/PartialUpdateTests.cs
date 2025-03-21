﻿#region References

using System;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PartialUpdateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FromJsonThenToModel()
	{
		var json = "{\"createdOn\":\"2000-01-02T03:04:00Z\",\"id\":2,\"isDeleted\":true,\"modifiedOn\":\"2000-01-02T03:04:01Z\",\"name\":\"Test\",\"syncId\":\"a28f0b1b-20b3-404d-b034-18fad78fd221\"}";
		var expected = new Test
		{
			CreatedOn = StartDateTime,
			Name = "Test",
			IsDeleted = true,
			SyncId = new Guid("A28F0B1B-20B3-404D-B034-18FAD78FD221"),
			ModifiedOn = StartDateTime.AddSeconds(1),
			Id = 2
		};

		var update = json.FromJson<PartialUpdate<Test>>();
		var actual = update.ToModel();

		AreEqual(expected, actual);
	}

	[TestMethod]
	public void FromModelThenToJson()
	{
		var model = new Test
		{
			CreatedOn = StartDateTime,
			Name = "Test",
			IsDeleted = true,
			SyncId = new Guid("A28F0B1B-20B3-404D-B034-18FAD78FD221"),
			ModifiedOn = StartDateTime.AddSeconds(1),
			Id = 2
		};

		var update = new PartialUpdate<Test>();
		update.FromModel(model);

		var actual = update.ToJson();
		var expected = "{\"CreatedOn\":\"2000-01-02T03:04:00Z\",\"Id\":2,\"IsDeleted\":true,\"ModifiedOn\":\"2000-01-02T03:04:01Z\",\"Name\":\"Test\",\"SyncId\":\"a28f0b1b-20b3-404d-b034-18fad78fd221\"}";

		AreEqual(expected, actual);
	}

	[TestMethod]
	public void GetTargetProperties()
	{
		var update = new PartialUpdate();
		var actual = update.GetTargetProperties().Select(x => x.Name).ToArray();
		var expected = Array.Empty<string>();
		AreEqual(expected, actual);

		update = new PartialUpdate<Test>();
		actual = update.GetTargetProperties().Select(x => x.Name).ToArray();
		expected = ["CreatedOn", "Id", "IsDeleted", "ModifiedOn", "Name", "SyncId"];
		AreEqual(expected, actual, $"\"{string.Join("\", \"", actual)}\"");
	}

	#endregion

	#region Classes

	private class Test : SyncEntity<long>
	{
		#region Properties

		/// <inheritdoc />
		public override long Id { get; set; }

		public string Name { get; set; }

		#endregion
	}

	#endregion
}