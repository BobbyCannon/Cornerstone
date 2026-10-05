using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Cornerstone.Presentation.OpenGL;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Vulkan;
using Cornerstone.Presentation.Platforms.Windows.DComposition;
using Cornerstone.Presentation.Platforms.Windows.DirectX;
using Cornerstone.Presentation.Platforms.Windows.OpenGl;
using Cornerstone.Presentation.Platforms.Windows.OpenGl.Angle;
using Cornerstone.Presentation.Platforms.Windows.Vulkan;
using Cornerstone.Presentation.Platforms.Windows.WinRT.Composition;

namespace Cornerstone.Presentation.Platforms.Windows;

static class Win32GlManager
{
    public static IPlatformGraphics? Initialize()
    {
        var gl = InitializeCore();

        if (gl is not null)
        {
            PresentationLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(gl);
        }
        if (gl is IPlatformGraphicsOpenGlContextFactory openGlFactory)
        {
            PresentationLocator.CurrentMutable.Bind<IPlatformGraphicsOpenGlContextFactory>().ToConstant(openGlFactory);
        }

        return gl;
    }
        
    private static IPlatformGraphics? InitializeCore()
    {
        var opts = PresentationLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions();
        if (opts.RenderingMode is null || !opts.RenderingMode.Any())
        {
            throw new InvalidOperationException($"{nameof(Win32PlatformOptions)}.{nameof(Win32PlatformOptions.RenderingMode)} must not be empty or null");
        }

        foreach (var renderingMode in opts.RenderingMode)
        {
            if (renderingMode == Win32RenderingMode.Software)
            {
                Win32Platform.ActiveRenderingMode = Win32RenderingMode.Software;
                return null;
            }

            if (renderingMode == Win32RenderingMode.AngleEgl)
            {
                var egl = AngleWin32PlatformGraphicsFactory.TryCreate(PresentationLocator.Current.GetService<AngleOptions>() ?? new());

                if (egl is D3D11AngleWin32PlatformGraphics)
                {
                    TryRegisterComposition(opts);
                    Win32Platform.ActiveRenderingMode = Win32RenderingMode.AngleEgl;
                    return egl;
                }
            }

            if (renderingMode == Win32RenderingMode.Wgl)
            {
                if (WglPlatformOpenGlInterface.TryCreate() is { } wgl)
                {
                    Win32Platform.ActiveRenderingMode = Win32RenderingMode.Wgl;
                    return wgl;
                }
            }

            if (renderingMode == Win32RenderingMode.Vulkan)
            {
                var vulkan = VulkanSupport.TryInitialize(PresentationLocator.Current.GetService<VulkanOptions>() ?? new());
                if (vulkan != null)
                {
                    Win32Platform.ActiveRenderingMode = Win32RenderingMode.Vulkan;
                    return vulkan;
                }
            }
        }

        throw new InvalidOperationException($"{nameof(Win32PlatformOptions)}.{nameof(Win32PlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
    }

    private static void TryRegisterComposition(Win32PlatformOptions opts)
    {
        if (opts.CompositionMode is null || !opts.CompositionMode.Any())
        {
            throw new InvalidOperationException($"{nameof(Win32PlatformOptions)}.{nameof(Win32PlatformOptions.CompositionMode)} must not be empty or null");
        }

        foreach (var compositionMode in opts.CompositionMode)
        {
            if (compositionMode == Win32CompositionMode.RedirectionSurface)
            {
                return;
            }

            if (compositionMode == Win32CompositionMode.WinUIComposition
                && WinUiCompositorConnection.IsSupported()
                && WinUiCompositorConnection.TryCreateAndRegister())
            {
                return;
            }

            if (compositionMode == Win32CompositionMode.DirectComposition
                && DirectCompositionConnection.IsSupported()
                && DirectCompositionConnection.TryCreateAndRegister())
            {
                return;
            }

            if (compositionMode == Win32CompositionMode.LowLatencyDxgiSwapChain
                && DxgiConnection.TryCreateAndRegister())
            {
                return;
            }
        }
            
        throw new InvalidOperationException($"{nameof(Win32PlatformOptions)}.{nameof(Win32PlatformOptions.CompositionMode)} has a value of \"{string.Join(", ", opts.CompositionMode)}\", but no options were applied.");
    }
}
