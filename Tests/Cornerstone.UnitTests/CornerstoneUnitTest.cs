#region References

using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Newtonsoft;
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