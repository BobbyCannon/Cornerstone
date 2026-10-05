#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

[TestClass]
public class SkiaOptionsTests
{
	#region Methods

	// Stencil buffers let Skia choose multisample-based path rendering, which quantizes edge
	// coverage and visibly degraded vector geometry anti-aliasing on GPU backends in 12.1.0.
	// They are a performance opt-in, so anything other than an explicit `true` must avoid them.
	[PresentationTestMethod]
	[DataRow(null, true)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	public void StencilBuffersAreAvoidedUnlessExplicitlyEnabled(bool? useStencilBuffers, bool expected)
	{
		CornerstoneTest.AreEqual(expected, SkiaOptions.ShouldAvoidStencilBuffers(useStencilBuffers));
	}

	#endregion
}