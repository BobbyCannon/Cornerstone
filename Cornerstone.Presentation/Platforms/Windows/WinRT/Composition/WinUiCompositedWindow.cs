using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Cornerstone.Presentation;
using Cornerstone.Presentation.OpenGL.Egl;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Rendering.Composition;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.Windows.WinRT.Composition;

internal class WinUiCompositedWindow : IDisposable
{
    public EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }
    private readonly WinUiCompositionShared _shared;
    private readonly ICompositionRoundedRectangleGeometry? _compositionRoundedRectangleGeometry;
    private readonly IVisual? _micaLight;
    private readonly IVisual? _micaDark;
    private readonly IVisual _blur;
    private readonly IVisual _visual;
    private readonly IVisualCollection _rootChildren;
    private readonly List<IVisual> _backdropTiles = new();
    private PixelSize _size;
    private BlurEffect _blurEffect;
    private BlurEffect? _appliedBlurEffect;
    private readonly ICompositionSurfaceBrush _surfaceBrush;
    private readonly ICompositionTarget _target;

    public void Dispose()
    {
        lock (_shared.SyncRoot)
        {
            _compositionRoundedRectangleGeometry?.Dispose();
            ClearBackdropTiles();
            _rootChildren.Dispose();
            _blur.Dispose();
            _micaLight?.Dispose();
            _micaDark?.Dispose();
            _visual.Dispose();
            _surfaceBrush.Dispose();
            _target.Dispose();
        }
    }

    public WinUiCompositedWindow(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info,
        WinUiCompositionShared shared, float? backdropCornerRadius, bool compositionAboveChildWindows = false)
    {
        WindowInfo = info;
        _shared = shared;
        // Raising this tree above child HWNDs on the frame does not show live native
        // through holes (stale DWM snapshot). The composition overlay has no child
        // HWNDs, so isTopmost stays false.
        int isTopmost = compositionAboveChildWindows ? 1 : 0;
        using var desktopTarget = shared.DesktopInterop.CreateDesktopWindowTarget(WindowInfo.Handle, isTopmost);
        _target = desktopTarget.QueryInterface<ICompositionTarget>();

        
        using var container = shared.Compositor.CreateContainerVisual();
        using var containerVisual = container.QueryInterface<IVisual>();
        using var containerVisual2 = container.QueryInterface<IVisual2>();
        containerVisual2.SetRelativeSizeAdjustment(new Vector2(1, 1));
        _rootChildren = container.Children;

        _target.SetRoot(containerVisual);

        _blur = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.BlurBrush);
        if (shared.MicaBrushLight != null)
        {
            _micaLight = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.MicaBrushLight);
            _rootChildren.InsertAtTop(_micaLight);
        }   
        
        if (shared.MicaBrushDark != null)
        {
            _micaDark = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.MicaBrushDark);
            _rootChildren.InsertAtTop(_micaDark);
        }

        _compositionRoundedRectangleGeometry =
            WinUiCompositionUtils.ClipVisual(shared.Compositor, backdropCornerRadius, _blur, _micaLight, _micaDark);

        _rootChildren.InsertAtTop(_blur);
        using var spriteVisual = shared.Compositor.CreateSpriteVisual();
        _visual = spriteVisual.QueryInterface<IVisual>();
        _rootChildren.InsertAtTop(_visual);

        _surfaceBrush = shared.Compositor.CreateSurfaceBrush();
        using var compositionBrush = _surfaceBrush.QueryInterface<ICompositionBrush>();
        spriteVisual.SetBrush(compositionBrush);

        _target.SetRoot(containerVisual);
    }

    public void SetSurface(ICompositionSurface surface) => _surfaceBrush.SetSurface(surface);

    // Called from the render thread with the transaction (SyncRoot) held, so effect changes
    // are applied within the same composition batch as the frame they belong to.
    public void ApplyEffects(CompositionTransparencyLevel transparencyLevel, PlatformThemeVariant themeVariant)
    {
        Debug.Assert(Monitor.IsEntered(_shared.SyncRoot));

        var blurEffect = transparencyLevel switch
        {
            CompositionTransparencyLevel.AcrylicBlur => BlurEffect.Acrylic,
            CompositionTransparencyLevel.Mica when themeVariant == PlatformThemeVariant.Dark => BlurEffect.MicaDark,
            CompositionTransparencyLevel.Mica => BlurEffect.MicaLight,
            _ => BlurEffect.None
        };
        if (_appliedBlurEffect == blurEffect)
            return;

        _blurEffect = blurEffect;
        _appliedBlurEffect = blurEffect;
        ApplyFullBackdropVisibility();
    }

    public void ApplyBackdropHoles(NativeAirspaceHole[] holes)
    {
        if (_size.Width <= 0 || _size.Height <= 0)
        {
            ApplyFullBackdropVisibility();
            return;
        }

        if (!NativeAirspace.BehindComposition || holes == null || holes.Length == 0)
        {
            ClearBackdropTiles();
            ApplyFullBackdropVisibility();
            return;
        }

        var holeBounds = new List<LtrbRect>(holes.Length);
        for (var i = 0; i < holes.Length; i++)
        {
            if (!holes[i].IsEmpty)
                holeBounds.Add(holes[i].PhysicalBounds);
        }

        if (holeBounds.Count == 0)
        {
            ClearBackdropTiles();
            ApplyFullBackdropVisibility();
            return;
        }

        var remainders = NativeAirspaceClip.SubtractHoles(
            new LtrbRect(0, 0, _size.Width, _size.Height), holeBounds);
        var brush = GetActiveBackdropBrush();
        if (brush == null || remainders.Length == 0)
        {
            ClearBackdropTiles();
            ApplyFullBackdropVisibility();
            return;
        }

        _blur.SetIsVisible(0);
        _micaLight?.SetIsVisible(0);
        _micaDark?.SetIsVisible(0);
        ClearBackdropTiles();
        for (var i = 0; i < remainders.Length; i++)
        {
            var rect = remainders[i];
            if (rect.IsEmpty)
                continue;
            var tile = WinUiCompositionUtils.CreateBackdropTileVisual(_shared.Compositor, brush,
                new Vector2((float)rect.Left, (float)rect.Top),
                new Vector2((float)rect.Width, (float)rect.Height));
            _rootChildren.InsertBelow(tile, _visual);
            _backdropTiles.Add(tile);
        }
    }

    private ICompositionBrush GetActiveBackdropBrush()
    {
        if (_blurEffect == BlurEffect.Acrylic)
            return _shared.BlurBrush;
        if (_blurEffect == BlurEffect.MicaLight)
            return _shared.MicaBrushLight ?? _shared.BlurBrush;
        if (_blurEffect == BlurEffect.MicaDark)
            return _shared.MicaBrushDark ?? _shared.BlurBrush;
        return null;
    }

    private void ApplyFullBackdropVisibility()
    {
        _blur.SetIsVisible(_blurEffect == BlurEffect.Acrylic
                           || (_blurEffect == BlurEffect.MicaLight && _micaLight == null) ||
                           (_blurEffect == BlurEffect.MicaDark && _micaDark == null) ?
            1 :
            0);
        _micaLight?.SetIsVisible(_blurEffect == BlurEffect.MicaLight ? 1 : 0);
        _micaDark?.SetIsVisible(_blurEffect == BlurEffect.MicaDark ? 1 : 0);
    }

    private void ClearBackdropTiles()
    {
        for (var i = 0; i < _backdropTiles.Count; i++)
        {
            _rootChildren.Remove(_backdropTiles[i]);
            _backdropTiles[i].Dispose();
        }

        _backdropTiles.Clear();
    }

    public IDisposable BeginTransaction()
    {
        Monitor.Enter(_shared.SyncRoot);
        return Disposable.Create(() => Monitor.Exit(_shared.SyncRoot));
    }

    public void ResizeIfNeeded(PixelSize size)
    {
        Debug.Assert(Monitor.IsEntered(_shared.SyncRoot));

        if (_size != size)
        {
            _visual.SetSize(new Vector2(size.Width, size.Height));
            _compositionRoundedRectangleGeometry?.SetSize(new Vector2(size.Width, size.Height));
            _size = size;
            ApplyBackdropHoles(NativeAirspace.GetHoles(WindowInfo));
        }
    }
}
