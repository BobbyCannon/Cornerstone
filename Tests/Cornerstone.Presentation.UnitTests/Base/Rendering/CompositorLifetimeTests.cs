#region References

using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

[TestClass]
public class CompositorLifetimeTests : CompositorTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void InvalidateVisualDoesNotUpdateRenderingTargetWhenRenderingStopped()
	{
		using var services = new CompositorTestServices(new Size(200, 200));

		var presentationSource = services.TopLevel.GetPresentationSource();
		CornerstoneTest.IsNotNull(presentationSource);

		var compositionTarget = ((CompositingRenderer) presentationSource.Renderer).CompositionTarget;
		CornerstoneTest.IsTrue(compositionTarget.IsEnabled);
		CornerstoneTest.AreEqual(new Size(200, 200), compositionTarget.Size);

		// Stop rendering and invalidate a visual: this should not result in an update
		services.TopLevel.StopRendering();
		((CompositorTestServices.TopLevelImpl) services.TopLevel.PlatformImpl!).ClientSize = new Size(300, 300);
		services.TopLevel.InvalidateVisual();
		services.RunJobs();

		CornerstoneTest.AreEqual(new Size(200, 200), compositionTarget.Size);

		// Check that restarting rendering re-queues the pending invalidation
		services.TopLevel.StartRendering();
		services.RunJobs();

		CornerstoneTest.AreEqual(new Size(300, 300), compositionTarget.Size);
	}

	#endregion
}