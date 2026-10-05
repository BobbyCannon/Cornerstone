#region References

using Cornerstone.Storage.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

[TestClass]
public class ConnectionStringParserTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetDatabaseName()
	{
		var scenarios = new (string Expected, string ConnectionString)[]
		{
			("Foo-Bar", "Data Source=Foo-Bar;Mode=Memory;Cache=Shared;"),
			("Hello-World", "server=localhost;database=Hello-World;integrated security=true;encrypt=false;"),
			("Hello-World", "Data Source=localhost;Initial Catalog=Hello-World;integrated security=true;encrypt=false;"),
			("Hello-World", "Data Source=localhost;Database=Hello-World;integrated security=true;encrypt=false;"),
			("Hello-World", "Server=localhost;Initial Catalog=\"Hello-World\";Integrated Security=true;Encrypt=false;"),
			("C:\\data\\app.mdf", "Data Source=localhost;AttachDbFilename=C:\\data\\app.mdf;Integrated Security=true;"),
			("Foo-Bar", "Filename=Foo-Bar;Mode=Memory;Cache=Shared;")
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Expected, ConnectionStringParser.GetDatabaseName(scenario.ConnectionString));
		}
	}

	[TestMethod]
	public void GetMasterString()
	{
		var scenarios = new (string Expected, string ConnectionString)[]
		{
			("server=localhost;database=master;integrated security=true;encrypt=false;",
				"server=localhost;database=Hello-World;integrated security=true;encrypt=false;"),
			("server=localhost;integrated security=true;encrypt=false;Database=master;",
				"server=localhost;integrated security=true;encrypt=false;"),
			("Data Source=localhost;Initial Catalog=master;integrated security=true;encrypt=false;",
				"Data Source=localhost;Initial Catalog=Hello-World;integrated security=true;encrypt=false;"),
			("Data Source=localhost;Initial Catalog=master;integrated security=true;encrypt=false;",
				"Data Source=localhost;Initial Catalog=\"Hello-World\";integrated security=true;encrypt=false;")
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Expected, ConnectionStringParser.GetMasterString(scenario.ConnectionString));
		}
	}

	#endregion
}