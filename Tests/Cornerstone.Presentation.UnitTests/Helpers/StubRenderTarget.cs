#region References

using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubRenderTarget : IRenderTarget
{
	#region Properties

	public RenderTargetProperties Properties => default;

	#endregion

	#region Methods

	public IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing)
	{
		return new HeadlessPlatformRenderInterface.HeadlessDrawingContextStub();
	}

	public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
		out RenderTargetDrawingContextProperties properties)
	{
		properties = default;
		return new HeadlessPlatformRenderInterface.HeadlessDrawingContextStub();
	}

	public void Dispose()
	{
	}

	#endregion
}

public sealed class StubRuntimePlatform : IRuntimePlatform
{
	#region Methods

	public RuntimePlatformInfo GetRuntimeInfo()
	{
		return new RuntimePlatformInfo { IsDesktop = true };
	}

	#endregion
}