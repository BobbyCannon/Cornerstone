#region References

using System;
using System.IO;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Newtonsoft;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Testing;
using Cornerstone.UnitTests.Resources;
using Cornerstone.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NUnit.Framework;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;

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

		DependencyInjector = new DependencyInjector();
		SetupDependencyInjection();
	}

	static CornerstoneUnitTest()
	{
		var assembly = typeof(CornerstoneUnitTest).Assembly;
		var assemblyDirectory = assembly.GetAssemblyDirectory();

		UnitTestsDirectory = assemblyDirectory.Parent?.Parent?.Parent?.FullName;
		UnitTestsFilePath = Path.Join(UnitTestsDirectory, $"{new DirectoryInfo(UnitTestsDirectory).Name}.csproj");
		SolutionDirectory = assemblyDirectory.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
		TempDirectory = Path.Combine(Path.GetTempPath(), "Cornerstone.UnitTests");
		EnableFileUpdates = false;

		SetClipboardService(new WindowsClipboardService());

		// Initialize the newtonsoft serializer settings.
		var jsonSettings = new JsonSerializerSettings();
		jsonSettings.InitializeSettings(Serializer.DefaultSettings, true);

		_ = new RuntimeInformation();
	}

	#endregion

	#region Properties

	public DependencyInjector DependencyInjector { get; }

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
		new DirectoryInfo(TempDirectory).SafeCreate();
		base.TestInitialize();
	}

	protected T Disposable<T>(Func<T> getDisposable) where T : IDisposable
	{
		var response = getDisposable();
		_disposables.Add(response);
		return response;
	}

	protected T GetModel<T>(Action<T> update = null) where T : new()
	{
		var response = new T();

		switch (response)
		{
			case AddressEntity sValue:
			{
				sValue.City = "City";
				sValue.Line1 = "Line1";
				sValue.Line2 = "Line2";
				sValue.Postal = "Postal";
				sValue.State = "State";
				break;
			}
			case ClientAccount sValue:
			{
				sValue.EmailAddress = "john@doe.com";
				sValue.Name = "John Doe";
				sValue.LastClientUpdate = DateTime.MinValue;
				break;
			}
			case ClientAddress sValue:
			{
				sValue.City = "City";
				sValue.Line1 = "Line1";
				sValue.Line2 = "Line2";
				sValue.Postal = "Postal";
				sValue.State = "State";
				break;
			}
			case SampleAccount sValue:
			{
				sValue.EmailAddress = "john@doe.com";
				sValue.FirstName = "John";
				sValue.LastName = "Doe";
				sValue.LastClientUpdate = DateTime.MinValue;
				break;
			}
		}

		update?.Invoke(response);

		return response;
	}

	#endregion
}