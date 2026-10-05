#region References

using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

public abstract class CornerstoneUnitTest : CornerstoneTest
{
	#region Properties

	/// <summary>
	/// When true, scenario matrices also run the SQL mapper and EF against localhost SQL Server.
	/// Leave false so unit tests stay on SQLite.
	/// </summary>
	protected virtual bool IncludeSqlServerDatabase => true;

	#endregion

	#region Methods

	[TestCleanup]
	public override void TestCleanup()
	{
		base.TestCleanup();
	}

	[TestInitialize]
	public override void TestInitialize()
	{
		base.TestInitialize();
	}

	#endregion
}