#region References

using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public static class RendererMocks
{
	#region Methods

	public static Compositor CreateDummyCompositor(IRenderTimer renderTimer = null)
	{
		return new(RenderLoop.FromTimer(renderTimer ?? new CompositorTestServices.ManualRenderTimer()), null, false,
			new CompositionCommitScheduler(), true, Dispatcher.UIThread);
	}

	internal static NullRenderer CreateRenderer()
	{
		return new NullRenderer();
	}

	#endregion

	#region Classes

	private class CompositionCommitScheduler : ICompositorScheduler
	{
		#region Methods

		public void CommitRequested(Compositor compositor)
		{
			if (PresentationLocator.Current.GetService<IPlatformRenderInterface>() == null)
			{
				return;
			}

			Dispatcher.UIThread.Post(() => compositor.Commit(), DispatcherPriority.AfterRender);
		}

		#endregion
	}

	#endregion
}