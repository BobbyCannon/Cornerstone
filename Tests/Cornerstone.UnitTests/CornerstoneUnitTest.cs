#region References

using System;
using System.IO;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Newtonsoft;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Testing;
using Cornerstone.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests;

public abstract partial class CornerstoneUnitTest : CornerstoneTest
{
	#region Fields

	private readonly SpeedyList<IDisposable> _disposables;

	#endregion

	#region Constructors

	protected CornerstoneUnitTest()
	{
		_disposables = [];

		Dependencies = new DependencyProvider("Unit Test");
	}

	static CornerstoneUnitTest()
	{
		var assembly = typeof(CornerstoneUnitTest).Assembly;
		var assemblyDirectory = assembly.GetAssemblyDirectory();

		UnitTestsDirectory = assemblyDirectory.Parent?.Parent?.Parent?.FullName;
		UnitTestsFilePath = Path.Join(UnitTestsDirectory, $"{new DirectoryInfo(UnitTestsDirectory).Name}.csproj");
		SolutionDirectory = assemblyDirectory.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
		TempDirectory = Path.Combine(Path.GetTempPath(), "Cornerstone.UnitTests");
		
		// Test settings
		EnableBrowserSamples = false;
		EnableFileUpdates = false;

		SetClipboardService(new WindowsClipboardService());

		// Initialize the newtonsoft serializer settings.
		var jsonSettings = new JsonSerializerSettings();
		jsonSettings.InitializeSettings(Serializer.DefaultSettings, true);

		_ = new RuntimeInformation();
	}

	#endregion

	#region Properties

	public DependencyProvider Dependencies { get; }

	/// <summary>
	/// Allows test data be displayed in the browser.
	/// </summary>
	public static bool EnableBrowserSamples { get; }

	public static bool EnableFileUpdates { get; }

	public static string SolutionDirectory { get; }

	public static string TempDirectory { get; }

	public static string UnitTestsDirectory { get; }

	public static string UnitTestsFilePath { get; }

	#endregion

	#region Methods

	[TearDown]
	[TestCleanup]
	public override void TestCleanup()
	{
		_disposables.ForEach(x => x.Dispose());
		_disposables.Clear();
		base.TestCleanup();
	}

	[SetUp]
	[TestInitialize]
	public override void TestInitialize()
	{
		SetupDependencyInjection();
		new DirectoryInfo(TempDirectory).SafeCreate();
		base.TestInitialize();
	}

	/// <summary>
	/// Dump the value to the console.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <param name="prefix"> An optional prefix to the dump. </param>
	public static T WriteLine<T>(T value, string prefix = null)
	{
		if (prefix != null)
		{
			Console.Write(prefix);
		}

		Console.WriteLine(value);
		return value;
	}

	protected T Disposable<T>(Func<T> getDisposable) where T : IDisposable
	{
		var response = getDisposable();
		_disposables.Add(response);
		return response;
	}

	#endregion
}