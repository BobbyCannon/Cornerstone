#region References

using System;
using System.IO;
using Cornerstone.Presentation.ApplicationUpdate;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class ApplicationUpdateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IsRequestedRequiresDestination()
	{
		IsFalse(ApplicationUpdate.IsRequested(null));

		var arguments = new ApplicationArguments();
		IsFalse(ApplicationUpdate.IsRequested(arguments));

		arguments.Parse(["-Update"]);
		IsFalse(ApplicationUpdate.IsRequested(arguments));

		arguments.Parse(["-Update", @"C:\Data\Software\Cornerstone.GrokMonitor"]);
		IsTrue(ApplicationUpdate.IsRequested(arguments));
	}

	[TestMethod]
	public void ProtectedDestinationsAreRejected()
	{
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\Windows"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\Windows\System32"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\Program Files\App"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\Program Files (x86)\App"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"D:\Users\Bobby"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\ProgramData"));
		IsTrue(ApplicationUpdate.IsProtectedDestination(@"C:\System Volume Information"));
		IsFalse(ApplicationUpdate.IsProtectedDestination(@"C:\Data\Software\Cornerstone.GrokMonitor"));

		var blocked = Path.Combine(Path.GetTempPath(), "does-not-matter");
		var windowsFolder = @"C:\Windows\ApplicationUpdateTests-" + Guid.NewGuid().ToString("N");
		var result = ApplicationUpdate.TryApply(blocked, windowsFolder, "App.exe", null);
		AreEqual(ApplicationUpdateResult.ProtectedDestination, result);
		IsFalse(Directory.Exists(windowsFolder));
	}

	[TestMethod]
	public void MissingDestinationIsCreatedAndFilled()
	{
		var root = CreateRoot();
		try
		{
			var source = Path.Combine(root, "source");
			var destination = Path.Combine(root, "destination");
			Directory.CreateDirectory(source);
			WriteFile(source, "App.exe");
			WriteFile(source, "note.txt");

			var result = ApplicationUpdate.TryApply(source, destination, "App.exe", null);

			AreEqual(ApplicationUpdateResult.Success, result);
			IsTrue(File.Exists(Path.Combine(destination, "App.exe")));
			IsTrue(File.Exists(Path.Combine(destination, "note.txt")));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	[TestMethod]
	public void ForeignDestinationIsLeftUnchanged()
	{
		var root = CreateRoot();
		try
		{
			var source = Path.Combine(root, "source");
			var destination = Path.Combine(root, "destination");
			Directory.CreateDirectory(source);
			Directory.CreateDirectory(destination);
			WriteFile(source, "App.exe");
			WriteFile(destination, "other.txt");

			var result = ApplicationUpdate.TryApply(source, destination, "App.exe", null);

			AreEqual(ApplicationUpdateResult.DestinationNotApplication, result);
			IsTrue(File.Exists(Path.Combine(destination, "other.txt")));
			IsFalse(File.Exists(Path.Combine(destination, "App.exe")));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	[TestMethod]
	public void TooManyRemovalsLeavesDestinationUnchanged()
	{
		var root = CreateRoot();
		try
		{
			var source = Path.Combine(root, "source");
			var destination = Path.Combine(root, "destination");
			Directory.CreateDirectory(source);
			Directory.CreateDirectory(destination);
			WriteFile(source, "App.exe");
			WriteFile(source, "added.txt");
			WriteFile(destination, "App.exe");
			for (var i = 0; i < (ApplicationUpdate.MaximumRemovals + 1); i++)
			{
				WriteFile(destination, "extra-" + i + ".txt");
			}

			var result = ApplicationUpdate.TryApply(source, destination, "App.exe", null);

			AreEqual(ApplicationUpdateResult.TooManyRemovals, result);
			IsTrue(File.Exists(Path.Combine(destination, "extra-0.txt")));
			IsTrue(File.Exists(Path.Combine(destination, "extra-" + ApplicationUpdate.MaximumRemovals + ".txt")));
			IsFalse(File.Exists(Path.Combine(destination, "added.txt")));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	[TestMethod]
	public void ApplyCopiesSourceAndRemovesRightOnlyFiles()
	{
		var root = CreateRoot();
		try
		{
			var source = Path.Combine(root, "source");
			var destination = Path.Combine(root, "destination");
			Directory.CreateDirectory(source);
			Directory.CreateDirectory(destination);
			WriteFile(source, "App.exe");
			WriteFile(source, "added.txt");
			WriteFile(destination, "App.exe");
			WriteFile(destination, "stale.txt");

			var result = ApplicationUpdate.TryApply(source, destination, "App.exe", null);

			AreEqual(ApplicationUpdateResult.Success, result);
			IsTrue(File.Exists(Path.Combine(destination, "App.exe")));
			IsTrue(File.Exists(Path.Combine(destination, "added.txt")));
			IsFalse(File.Exists(Path.Combine(destination, "stale.txt")));
		}
		finally
		{
			DeleteRoot(root);
		}
	}

	private static string CreateRoot()
	{
		// Path.GetTempPath is under Users, which the updater refuses.
		var root = Path.Combine(@"C:\Workspaces\Builds", "ApplicationUpdateTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		return root;
	}

	private static void DeleteRoot(string root)
	{
		if (Directory.Exists(root))
		{
			Directory.Delete(root, true);
		}
	}

	private static void WriteFile(string directory, string name)
	{
		File.WriteAllText(Path.Combine(directory, name), name);
	}

	#endregion
}
