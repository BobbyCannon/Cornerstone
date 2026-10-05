#region References

using System;
using System.Reflection;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Internal;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class AssetLoaderTests
{
	#region Constants

	private const string AssemblyNameWithNonAscii = "Какое-то-название";

	private const string AssemblyNameWithWhitespace = "Awesome Library";

	#endregion

	#region Fields

	private readonly StubAssemblyDescriptorResolver _resolver;

	#endregion

	#region Constructors

	public AssetLoaderTests()
	{
		// Process-wide. Apps and most tests register this once; without it this class is
		// order-dependent (see upstream #2555). Register here so csres hosts match production.
		AssetLoader.RegisterResUriParsers();

		_resolver = new StubAssemblyDescriptorResolver();

		var descriptor = CreateAssemblyDescriptor(AssemblyNameWithWhitespace);
		_resolver.SetAssembly(AssemblyNameWithWhitespace, descriptor);

		descriptor = CreateAssemblyDescriptor(AssemblyNameWithNonAscii);
		_resolver.SetAssembly(AssemblyNameWithNonAscii, descriptor);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AssemblyNameWithNonASCIIShouldLoadCsres()
	{
		var uri = new Uri($"csres://{AssemblyNameWithNonAscii}/Assets/something");
		var loader = new StandardAssetLoader(_resolver);

		var assemblyActual = loader.GetAssembly(uri, null);

		CornerstoneTest.AreEqual(AssemblyNameWithNonAscii, assemblyActual?.FullName);
	}

	[PresentationTestMethod]
	public void AssemblyNameWithWhitespaceShouldLoadResm()
	{
		var uri = new Uri($"resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.something?assembly={AssemblyNameWithWhitespace}");
		var loader = new StandardAssetLoader(_resolver);

		var assemblyActual = loader.GetAssembly(uri, null);

		CornerstoneTest.AreEqual(AssemblyNameWithWhitespace, assemblyActual?.FullName);
	}

	[PresentationTestMethod]
	public void InvalidAssemblyNameShouldYieldEmptyEnumerable()
	{
		var uri = new Uri("csres://InvalidAssembly");
		var loader = new StandardAssetLoader(_resolver);

		var assemblyActual = loader.GetAssets(uri, null);

		CornerstoneTest.Empty(assemblyActual);
	}

	private static IAssemblyDescriptor CreateAssemblyDescriptor(string assemblyName)
	{
		return new StubAssemblyDescriptor(new StubNamedAssembly(assemblyName));
	}

	#endregion

	#region Classes

	public class MockAssembly : Assembly
	{
	}

	#endregion
}