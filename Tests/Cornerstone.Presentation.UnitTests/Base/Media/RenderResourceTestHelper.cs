#region References

using System;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

internal class RenderResourceTestHelper : IDisposable
{
	#region Properties

	public Compositor Compositor => Services.Compositor;
	public CompositorTestServices Services { get; } = new();

	#endregion

	#region Methods

	public void AddToCompositor(ICompositionRenderResource resource)
	{
		resource.AddRefOnCompositor(Compositor);
	}

	public void AssertExistsOnCompositor(ICompositorSerializable resource, bool exists = true)
	{
		var server = resource.TryGetServer(Compositor);
		if (exists)
		{
			CornerstoneTest.IsNotNull(server);
		}
		else
		{
			CornerstoneTest.IsNull(server);
		}
	}

	public void AssertInvalidation<T>(T resource, Action cb)
		where T : ICompositionRenderResource, ICompositorSerializable
	{
		resource.AddRefOnCompositor(Compositor);
		CornerstoneTest.IsNotNull(resource.TryGetServer(Compositor));
		CornerstoneTest.IsTrue(Compositor.UnitTestIsRegisteredForSerialization(resource));

		Compositor.Commit();
		Compositor.Server.Render(false);

		CornerstoneTest.IsFalse(Compositor.UnitTestIsRegisteredForSerialization(resource));
		cb();
		CornerstoneTest.IsTrue(Compositor.UnitTestIsRegisteredForSerialization(resource));
		resource.ReleaseOnCompositor(Compositor);
		CornerstoneTest.IsNull(resource.TryGetServer(Compositor));
	}

	public static void AssertResourceInvalidation<T>(T resource, Action cb)
		where T : ICompositionRenderResource, ICompositorSerializable
	{
		using var helper = new RenderResourceTestHelper();
		helper.AssertInvalidation(resource, cb);
	}

	public void Dispose()
	{
		Services.Dispose();
	}

	public bool IsInvalidated(ICompositorSerializable resource)
	{
		return Compositor.UnitTestIsRegisteredForSerialization(resource);
	}

	#endregion
}