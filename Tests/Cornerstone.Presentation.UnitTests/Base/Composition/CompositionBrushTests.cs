#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Composition;

[TestClass]
public class CompositionBrushTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingSpreadMethodAfterACommitShouldReachTheServer()
	{
		using var services = new CompositorTestServices();
		var compositor = services.Compositor;

		var brush = compositor.CreateLinearGradientBrush();
		brush.GradientStops.Add(compositor.CreateGradientStop(0, Colors.Red));
		services.RunJobs();

		CornerstoneTest.AreEqual(GradientSpreadMethod.Pad, brush.Server.SpreadMethod);

		brush.SpreadMethod = GradientSpreadMethod.Repeat;
		services.RunJobs();

		CornerstoneTest.AreEqual(GradientSpreadMethod.Repeat, brush.Server.SpreadMethod);
	}

	[PresentationTestMethod]
	public void MutableGradientStopsShouldBeSnapshottedForTheServer()
	{
		using var services = new CompositorTestServices();
		var compositor = services.Compositor;

		var mutableStop = new GradientStop(Colors.Red, 0);
		var brush = compositor.CreateLinearGradientBrush();
		brush.GradientStops.Add(mutableStop);
		services.RunJobs();

		// The render thread reads the server list at replay time, so a mutable
		// UI-thread stop must not cross the batch by reference.
		var serverStop = CornerstoneTest.Single(brush.Server.GradientStops);
		CornerstoneTest.NotSame(mutableStop, serverStop);
		CornerstoneTest.AreEqual(Colors.Red, serverStop.Color);
		CornerstoneTest.AreEqual(0, serverStop.Offset);
	}

	[PresentationTestMethod]
	public void ReplacingTheGradientStopListAfterACommitShouldReachTheServer()
	{
		using var services = new CompositorTestServices();
		var compositor = services.Compositor;

		var brush = compositor.CreateLinearGradientBrush();
		brush.GradientStops.Add(compositor.CreateGradientStop(0, Colors.Red));
		services.RunJobs();

		CornerstoneTest.Single(brush.Server.GradientStops);

		brush.GradientStops = new List<IGradientStop>
		{
			compositor.CreateGradientStop(0, Colors.Red),
			compositor.CreateGradientStop(1, Colors.Blue)
		};
		services.RunJobs();

		CornerstoneTest.AreEqual(2, brush.Server.GradientStops.Count);
	}

	[PresentationTestMethod]
	public void UsingACompositionBrushWithAForeignCompositorShouldThrow()
	{
		using var services = new CompositorTestServices();

		var brush = services.Compositor.CreateSolidColorBrush(Colors.Red);

		var foreign = new Compositor(RenderLoop.FromTimer(services.Timer), null,
			true, new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);

		// A composition brush's server object belongs to its own compositor's
		// render loop; handing it to another compositor's stream would let two
		// render threads race over one resource.
		Assert.Throws<InvalidOperationException>(() => brush.GetServer(foreign));

		// A transient context without a compositor keeps the client brush and
		// draws its static values, so no affinity applies there.
		CornerstoneTest.Same(brush, brush.GetServer(null));
	}

	#endregion
}