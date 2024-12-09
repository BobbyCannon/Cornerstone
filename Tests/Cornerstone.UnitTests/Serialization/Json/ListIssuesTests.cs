#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json;

[TestClass]
public class ListIssuesTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void PreventDuplicates()
	{
		//var s = new TestSettings();
		//s.DumpJson();

		var json = """
					{
						"SubSettings1": {
							"Id": 1,
							"SubIds": [
								"36053e69-48b2-4348-b42b-a64431fcb4ce",
								"0dc60e35-06db-4ca9-98d7-0dffb1e07e3b",
								"ac68e8b0-c75f-4a9e-a5aa-4d90ca780add"
							]
						},
						"SubSettings2": {
							"Id": 1,
							"SubIds": [
								"36053e69-48b2-4348-b42b-a64431fcb4ce",
								"0dc60e35-06db-4ca9-98d7-0dffb1e07e3b",
								"ac68e8b0-c75f-4a9e-a5aa-4d90ca780add"
							]
						}
					}
					""";

		var actual = json.FromJson<TestSettings>();
		//var actual = JsonConvert.DeserializeObject<TestSettings>(json);
		actual.DumpJson();

		IsNotNull(actual.SubSettings1.SubIds as SpeedyList<Guid>);
	}

	#endregion
}

public class TestSettings
{
	#region Constructors

	public TestSettings()
	{
		Debug.WriteLine("+Constructor");
		SubSettings1 = SubTestSettings.Create();
		SubSettings2 = SubTestSettings.Create();
		Debug.WriteLine("-Constructor");
	}

	#endregion

	#region Properties

	public SubTestSettings SubSettings1 { get; set; }

	public SubTestSettings SubSettings2 { get; set; }

	#endregion
}

public class SubTestSettings : Notifiable
{
	#region Constructors

	public SubTestSettings()
	{
		SubIds = new SpeedyList<Guid>();
	}

	#endregion

	#region Properties

	public int Id { get; set; }

	public IList<Guid> SubIds { get; set; }

	#endregion

	#region Methods

	public static SubTestSettings Create()
	{
		var response = new SubTestSettings
		{
			Id = 1,
			SubIds = new SpeedyList<Guid>
			{
				DistinctCheck = (x, y) => x.Equals(y)
			}
		};
		response.SubIds.Add(
			new Guid("36053E69-48B2-4348-B42B-A64431FCB4CE"),
			new Guid("0DC60E35-06DB-4CA9-98D7-0DFFB1E07E3B"),
			new Guid("AC68E8B0-C75F-4A9E-A5AA-4D90CA780ADD")
		);

		return response;
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName = null)
	{
		Debug.WriteLine(propertyName);
		base.OnPropertyChanged(propertyName);
	}

	#endregion
}