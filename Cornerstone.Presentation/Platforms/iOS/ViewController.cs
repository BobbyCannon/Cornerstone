using System;
using Cornerstone.Presentation.Metadata;
using UIKit;

namespace Cornerstone.Presentation.iOS;

[Unstable]
public interface ICornerstoneViewController
{
#if !TVOS
    UIStatusBarStyle PreferredStatusBarStyle { get; set; }
#endif
    bool PrefersStatusBarHidden { get; set; }
    Thickness SafeAreaPadding { get; }
    event EventHandler? SafeAreaPaddingChanged;
}

/// <inheritdoc cref="ICornerstoneViewController" />
public class DefaultCornerstoneViewController : UIViewController, ICornerstoneViewController
{
#if !TVOS
    private UIStatusBarStyle? _preferredStatusBarStyle;
#endif
    private bool? _prefersStatusBarHidden;
    
    /// <inheritdoc/>
    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        var size = View?.Frame.Size ?? default;
        var frame = View?.SafeAreaLayoutGuide.LayoutFrame ?? default;
        var safeArea = new Thickness(frame.Left, frame.Top, size.Width - frame.Right, size.Height - frame.Bottom);
        if (SafeAreaPadding != safeArea)
        {
            SafeAreaPadding = safeArea;
            SafeAreaPaddingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

#if !TVOS
    /// <inheritdoc/>
    public override bool PrefersStatusBarHidden()
    {
        return _prefersStatusBarHidden ??= base.PrefersStatusBarHidden();
    }

    /// <inheritdoc/>
    public override UIStatusBarStyle PreferredStatusBarStyle()
    {
        // don't set _preferredStatusBarStyle value if it's null, so we can keep "default" there instead of actual app style.
        return _preferredStatusBarStyle ?? base.PreferredStatusBarStyle();
    }

    UIStatusBarStyle ICornerstoneViewController.PreferredStatusBarStyle
    {
        get => _preferredStatusBarStyle ?? UIStatusBarStyle.Default;
        set
        {
            _preferredStatusBarStyle = value;
            SetNeedsStatusBarAppearanceUpdate();
        }
    }
#endif

    bool ICornerstoneViewController.PrefersStatusBarHidden
    {
        get => _prefersStatusBarHidden ?? false; // false is default on ios/ipados
        set
        {
            _prefersStatusBarHidden = value;
#if !TVOS
            SetNeedsStatusBarAppearanceUpdate();
#endif
        }
    }

    /// <inheritdoc/>
    public Thickness SafeAreaPadding { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? SafeAreaPaddingChanged;
}
