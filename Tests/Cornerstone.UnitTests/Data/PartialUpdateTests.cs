#region References

using System;
using Cornerstone.Data;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PartialUpdateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetTargetProperties()
	{
		var update = new PartialUpdate();
		var actual = update.GetTargetProperties();
		var expected = Array.Empty<string>();
		AreEqual(expected, actual.Keys);

		update = new PartialUpdate<Test>();
		actual = update.GetTargetProperties();
		expected = ["Id", "Name", "CreatedOn", "IsDeleted", "ModifiedOn", "SyncId"];
		AreEqual(expected, actual.Keys, $"\"{string.Join("\", \"", actual.Keys)}\"");
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