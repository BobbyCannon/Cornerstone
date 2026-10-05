#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Acrylic;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class CompositorTestServices : IDisposable
{
	#region Fields

	private readonly IDisposable _app;

	#endregion

	#region Constructors

	public CompositorTestServices(Size? size = null, IPlatformRenderInterface renderInterface = null)
	{
		var services = TestServices.MockPlatformRenderInterface;
		if (renderInterface != null)
		{
			services = services.With(renderInterface: renderInterface);
		}

		_app = UnitTestApplication.Start(services);
		try
		{
			var renderLoop = RenderLoop.FromTimer(Timer);
			PresentationLocator.CurrentMutable.Bind<IRenderLoop>().ToConstant(renderLoop);

			Compositor = new Compositor(renderLoop, null,
				true, new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);
			var impl = new TopLevelImpl(Compositor, size ?? new Size(1000, 1000));
			TopLevel = new EmbeddableControlRoot(impl)
			{
				Template = new FuncControlTemplate((parent, scope) =>
				{
					var presenter = new ContentPresenter
					{
						[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
					};
					scope.Register("PART_ContentPresenter", presenter);
					return presenter;
				})
			};
			TopLevel.Prepare();
			TopLevel.StartRendering();
			RunJobs();
			Renderer = TopLevel.Renderer;
			Renderer.CompositionTarget.Server.DebugEvents = Events;
		}
		catch
		{
			_app.Dispose();
			throw;
		}
	}

	#endregion

	#region Properties

	public Compositor Compositor { get; }
	public DebugEvents Events { get; } = new();
	public ManualRenderTimer Timer { get; } = new();
	public EmbeddableControlRoot TopLevel { get; }
	internal CompositingRenderer Renderer { get; }

	#endregion

	#region Methods

	public void AssertHitTest(double x, double y, Func<Visual, bool> filter, params Visual[] expected)
	{
		AssertHitTest(new Point(x, y), filter, expected);
	}

	public void AssertHitTest(Point pt, Func<Visual, bool> filter, params Visual[] expected)
	{
		RunJobs();
		var tested = Renderer.HitTest(pt, TopLevel, filter);
		CornerstoneTest.AreEqual(expected, tested);
	}

	public void AssertHitTest(Geometry geometry, Func<Visual, bool> filter, params GeometryHitTestResult[] expected)
	{
		RunJobs();
		var tested = Renderer.HitTest(geometry, TopLevel, filter);
		CornerstoneTest.AreEqual(expected, tested);
	}

	public void AssertHitTestFirst(Point pt, Func<Visual, bool> filter, Visual expected)
	{
		RunJobs();
		var tested = Renderer.HitTestFirst(pt, TopLevel, filter);
		CornerstoneTest.AreEqual(expected, tested);
	}

	public void AssertHitTestFirst(Geometry geometry, Func<Visual, bool> filter, Visual expected)
	{
		RunJobs();
		var tested = Renderer.HitTestFirst(geometry, TopLevel, filter);
		CornerstoneTest.AreEqual(expected, tested?.VisualHit);
	}

	public void AssertRects(params Rect[] rects)
	{
		RunJobs();
		var toAssert = rects.Select(x => x.ToString()).Distinct().OrderBy(x => x);
		var invalidated = Events.Rects.Select(x => x.ToString()).Distinct().OrderBy(x => x);
		CornerstoneTest.AreEqual(toAssert, invalidated);
		Events.Rects.Clear();
	}

	public void AssertRenderedVisuals(int visitedVisuals, int renderVisuals)
	{
		RunJobs();
		CornerstoneTest.AreEqual(visitedVisuals, Events.VisitedVisuals);
		CornerstoneTest.AreEqual(renderVisuals, Events.RenderedVisuals);
		Events.Rects.Clear();
	}

	public void Dispose()
	{
		TopLevel.StopRendering();
		TopLevel.Dispose();
		_app.Dispose();
	}

	public void RunJobs()
	{
		Dispatcher.UIThread.RunJobs();
		Timer.TriggerTick();
		Dispatcher.UIThread.RunJobs();
	}

	#endregion

	#region Classes

	public class DebugEvents : ICompositionTargetDebugEvents
	{
		#region Fields

		public List<Rect> Rects = new();

		#endregion

		#region Properties

		public int RenderedVisuals { get; set; }
		public int VisitedVisuals { get; set; }

		#endregion

		#region Methods

		public void RectInvalidated(LtrbRect rc)
		{
			Rects.Add(rc.ToRect());
		}

		public void Reset()
		{
			Rects.Clear();
			RenderedVisuals = 0;
			VisitedVisuals = 0;
		}

		#endregion
	}

	public class ManualRenderTimer : IRenderTimer
	{
		#region Properties

		public bool RunsInBackground => false;
		public Action<TimeSpan> Tick { get; set; }

		#endregion

		#region Methods

		public void TriggerTick()
		{
			Tick?.Invoke(TimeSpan.Zero);
		}

		#endregion
	}

	public class TopLevelImpl : ITopLevelImpl
	{
		#region Constructors

		public TopLevelImpl(Compositor compositor, Size clientSize)
		{
			ClientSize = clientSize;
			Compositor = compositor;
		}

		#endregion

		#region Properties

		public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new(1, 1, 1);
		public Size ClientSize { get; set; }

		public Action Closed { get; set; }

		public Compositor Compositor { get; }

		public double DesktopScaling => 1;
		public IPlatformHandle Handle => null;
		public Action<RawInputEventArgs> Input { get; set; }
		public Action LostFocus { get; set; }
		public Action<Rect> Paint { get; set; }
		public double RenderScaling => 1;
		public Action<Size, WindowResizeReason> Resized { get; set; }
		public Action<double> ScalingChanged { get; set; }
		public IPlatformRenderSurface[] Surfaces { get; } = [new DummyFramebufferSurface()];

		public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;
		public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

		#endregion

		#region Methods

		public IPopupImpl CreatePopup()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
		}

		public Point PointToClient(PixelPoint point)
		{
			return default;
		}

		public PixelPoint PointToScreen(Point point)
		{
			return new();
		}

		public void SetCursor(ICursorImpl cursor)
		{
		}

		public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
		{
		}

		public void SetInputRoot(IInputRoot inputRoot)
		{
		}

		public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel)
		{
		}

		public object TryGetFeature(Type featureType)
		{
			return null;
		}

		#endregion

		#region Classes

		private class DummyFramebufferSurface : IFramebufferPlatformSurface
		{
			#region Methods

			public IFramebufferRenderTarget CreateFramebufferRenderTarget()
			{
				return new FuncFramebufferRenderTarget(Lock);
			}

			public ILockedFramebuffer Lock()
			{
				var ptr = Marshal.AllocHGlobal(128);
				return new LockedFramebuffer(ptr, new PixelSize(1, 1), 4, new Vector(96, 96),
					PixelFormat.Rgba8888, AlphaFormat.Premul, () => Marshal.FreeHGlobal(ptr));
			}

			#endregion
		}

		#endregion
	}

	#endregion
}

public class NullCompositorScheduler : ICompositorScheduler
{
	#region Methods

	public void CommitRequested(Compositor compositor)
	{
	}

	#endregion
}

public class DispatcherCompositorScheduler : ICompositorScheduler
{
	#region Methods

	public void CommitRequested(Compositor compositor)
	{
		Dispatcher.UIThread.Post(() => compositor.Commit(), DispatcherPriority.UiThreadRender);
	}

	#endregion
}