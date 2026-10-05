#region References

using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class StylesTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddingStyleShouldSetOwner()
	{
		var host = new StubResourceHost();
		var target = new Styles(host);
		var style = new StubStyle();

		host.Calls.Clear();
		target.Add(style);

		style.Calls.VerifyCalled("AddOwner");
		CornerstoneTest.Same(host, style.Owner);
	}

	[PresentationTestMethod]
	public void FindsResourceInMergedDictionary()
	{
		var target = new Styles
		{
			Resources = new ResourceDictionary
			{
				MergedDictionaries =
				{
					new ResourceDictionary
					{
						{ "foo", "bar" }
					}
				}
			}
		};

		CornerstoneTest.IsTrue(target.TryGetResource("foo", ThemeVariant.Dark, out var result));
		CornerstoneTest.AreEqual("bar", result);
	}

	[PresentationTestMethod]
	public void RemovingStyleShouldClearOwner()
	{
		var host = new StubResourceHost();
		var target = new Styles(host);
		var style = new StubStyle();

		host.Calls.Clear();
		target.Add(style);
		target.Remove(style);

		style.Calls.VerifyCalled("RemoveOwner");
		CornerstoneTest.IsNull(style.Owner);
	}

	[PresentationTestMethod]
	public void ShouldSetOwnerOnAssignedResources()
	{
		var host = new StubResourceHost();
		var target = new Styles();
		((IResourceProvider) target).AddOwner(host);

		var resources = new StubResourceDictionary();
		target.Resources = resources;

		resources.Calls.VerifyCalled("AddOwner", 1);
	}

	[PresentationTestMethod]
	public void ShouldSetOwnerOnAssignedResources2()
	{
		var host = new StubResourceHost();
		var target = new Styles();

		var resources = new StubResourceDictionary();
		target.Resources = resources;

		host.Calls.Clear();
		((IResourceProvider) target).AddOwner(host);
		resources.Calls.VerifyCalled("AddOwner", 1);
	}

	[PresentationTestMethod]
	public void ShouldSetOwnerOnChildStyle()
	{
		var host = new StubResourceHost();
		var target = new Styles();
		((IResourceProvider) target).AddOwner(host);

		var style = new StubStyle();
		target.Add(style);

		style.Calls.VerifyCalled("AddOwner", 1);
		CornerstoneTest.Same(host, style.Owner);
	}

	[PresentationTestMethod]
	public void ShouldSetOwnerOnChildStyle2()
	{
		var host = new StubResourceHost();
		var target = new Styles();

		var style = new StubStyle();
		target.Add(style);

		host.Calls.Clear();
		((IResourceProvider) target).AddOwner(host);
		style.Calls.VerifyCalled("AddOwner", 1);
		CornerstoneTest.Same(host, style.Owner);
	}

	#endregion
}