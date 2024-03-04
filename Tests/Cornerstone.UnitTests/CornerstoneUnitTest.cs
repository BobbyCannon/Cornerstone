#region References

using System;
using System.IO;
using System.Windows;
using Cornerstone.Extensions;
using Cornerstone.Newtonsoft;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Testing;
using Cornerstone.UnitTests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.UnitTests;

public abstract partial class CornerstoneUnitTest : CornerstoneTest
{
	#region Constructors

	static CornerstoneUnitTest()
	{
		var assembly = typeof(CornerstoneUnitTest).Assembly;
		var assemblyDirectory = assembly.GetAssemblyDirectory();

		UnitTestsDirectory = assemblyDirectory.Parent?.Parent?.Parent?.FullName;
		SolutionDirectory = assemblyDirectory.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
		TempDirectory = Path.Combine(Path.GetTempPath(), "Cornerstone.UnitTests");
		EnableFileUpdates = false;

		SetClipboardProvider(x => Clipboard.SetText(x ?? "null"));

		// Initialize the newtonsoft serializer settings.
		var jsonSettings = new JsonSerializerSettings();
		jsonSettings.InitializeSettings(Serializer.DefaultSettings, true);

		_ = new RuntimeInformation();
	}

	#endregion

	#region Properties

	public static bool EnableFileUpdates { get; }

	public static string SolutionDirectory { get; }

	public static string TempDirectory { get; }

	public static string UnitTestsDirectory { get; }

	#endregion

	#region Methods

	[TestInitialize]
	public override void TestInitialize()
	{
		new DirectoryInfo(TempDirectory).SafeCreate();
		base.TestInitialize();
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