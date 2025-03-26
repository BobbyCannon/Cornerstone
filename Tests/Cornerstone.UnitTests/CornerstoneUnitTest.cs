#region References

using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Newtonsoft;
using Cornerstone.Platforms.Windows;
using Cornerstone.Serialization;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests;

public abstract partial class CornerstoneUnitTest : CornerstoneTest
{
	#region Constructors

	protected CornerstoneUnitTest()
	{
		EnableFileUpdates = false;

		ClientSqlConnectionString = "server=localhost;database=CornerstoneClient;integrated security=true;encrypt=false";
		ClientSqlConnectionString2 = "server=localhost;database=CornerstoneClient2;integrated security=true;encrypt=false";
		ClientSqliteConnectionString = "Data Source=CornerstoneClient.db";
		ClientSqliteConnectionString2 = "Data Source=CornerstoneClient2.db";
		ServerSqlConnectionString = "server=localhost;database=CornerstoneServer;integrated security=true;encrypt=false";
		ServerSqlConnectionString2 = "server=localhost;database=CornerstoneServer2;integrated security=true;encrypt=false";
		ServerSqliteConnectionString = "Data Source=CornerstoneServer.db";
		ServerSqliteConnectionString2 = "Data Source=CornerstoneServer2.db";
	}

	static CornerstoneUnitTest()
	{
		var assembly = typeof(CornerstoneUnitTest).Assembly;
		var assemblyDirectory = assembly.GetAssemblyDirectory();

		EnableBrowserSamples = false;
		UnitTestsDirectory = assemblyDirectory.Parent?.Parent?.Parent?.FullName;
		UnitTestsFilePath = Path.Join(UnitTestsDirectory, $"{new DirectoryInfo(UnitTestsDirectory).Name}.csproj");
		SolutionDirectory = assemblyDirectory.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;

		// Initialize the newtonsoft serializer settings.
		var jsonSettings = new JsonSerializerSettings();
		jsonSettings.InitializeSettings(Serializer.DefaultSettings, true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Allows test data be displayed in the browser.
	/// </summary>
	public static bool EnableBrowserSamples { get; }

	public static string UnitTestsDirectory { get; }

	public static string UnitTestsFilePath { get; }

	#endregion

	#region Methods

	[TearDown]
	[TestCleanup]
	public override void TestCleanup()
	{
		base.TestCleanup();
	}

	[SetUp]
	[TestInitialize]
	public override void TestInitialize()
	{
		new DirectoryInfo(TempDirectory).SafeCreate();
		base.TestInitialize();
	}

	#endregion
}