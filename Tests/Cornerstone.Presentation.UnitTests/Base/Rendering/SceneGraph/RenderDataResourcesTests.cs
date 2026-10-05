#region References

using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering.SceneGraph;

[TestClass]
public class RenderDataResourcesTests
{
	#region Methods

	[PresentationTestMethod]
	public void AppendDeserializedAppendsWithoutDeduplication()
	{
		var resources = new RenderDataResources();
		var obj = new object();

		var first = resources.AppendDeserialized(obj);
		var second = resources.AppendDeserialized(obj);

		CornerstoneTest.AreNotEqual(first, second);
		CornerstoneTest.AreEqual(2, resources.Count);
		CornerstoneTest.Same(obj, resources[first]);
		CornerstoneTest.Same(obj, resources[second]);
	}

	[PresentationTestMethod]
	public void DisposeResetsTheTable()
	{
		var resources = new RenderDataResources();
		resources.Intern(new object());

		resources.Dispose();

		CornerstoneTest.AreEqual(0, resources.Count);
	}

	[PresentationTestMethod]
	public void IndexerNullHandleReturnsNull()
	{
		var resources = new RenderDataResources();
		resources.Intern(new object());

		CornerstoneTest.IsNull(resources[RenderDataResources.NullHandle]);
	}

	[PresentationTestMethod]
	public void IndexerReturnsInternedResource()
	{
		var resources = new RenderDataResources();
		var obj = new object();

		var handle = resources.Intern(obj);

		CornerstoneTest.Same(obj, resources[handle]);
	}

	[PresentationTestMethod]
	public void InternDistinctReferencesReturnDistinctHandles()
	{
		var resources = new RenderDataResources();
		var a = resources.Intern(new object());
		var b = resources.Intern(new object());

		CornerstoneTest.AreNotEqual(a, b);
		CornerstoneTest.AreEqual(2, resources.Count);
	}

	[PresentationTestMethod]
	public void InternEqualButDistinctReferencesReturnDistinctHandles()
	{
		var resources = new RenderDataResources();
		var a = resources.Intern(new EqualByValue());
		var b = resources.Intern(new EqualByValue());

		CornerstoneTest.AreNotEqual(a, b);
		CornerstoneTest.AreEqual(2, resources.Count);
	}

	[PresentationTestMethod]
	public void InternNullReturnsNullHandle()
	{
		var resources = new RenderDataResources();
		CornerstoneTest.AreEqual(RenderDataResources.NullHandle, resources.Intern(null));
		CornerstoneTest.AreEqual(0, resources.Count);
	}

	[PresentationTestMethod]
	public void InternSameReferenceReturnsSameHandle()
	{
		var resources = new RenderDataResources();
		var obj = new object();

		var first = resources.Intern(obj);
		var second = resources.Intern(obj);

		CornerstoneTest.AreEqual(first, second);
		CornerstoneTest.AreEqual(1, resources.Count);
	}

	#endregion

	#region Classes

	private sealed class EqualByValue
	{
		#region Methods

		public override bool Equals(object obj)
		{
			return obj is EqualByValue;
		}

		public override int GetHashCode()
		{
			return 1;
		}

		#endregion
	}

	#endregion
}