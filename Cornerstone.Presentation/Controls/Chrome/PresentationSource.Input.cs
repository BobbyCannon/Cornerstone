using System;
using System.Threading;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Controls.Chrome;

internal partial class PresentationSource
{
    public IInputRoot InputRoot => this;
    
    /// <summary>
    /// Handles input from <see cref="ITopLevelImpl.Input"/>.
    /// </summary>
    private void HandleInputCore(object? state)
    {
        using var _ = Diagnostic.BeginLayoutInputPass();

        var e = (RawInputEventArgs)state!;
        if (e is RawPointerEventArgs pointerArgs)
        {
            var hitTestElement = RootElement.InputHitTest(pointerArgs.Position, enabledElementsOnly: false);

            pointerArgs.InputHitTestResult = (hitTestElement, FirstEnabledAncestor(hitTestElement));
        }

        _inputManager?.ProcessInput(e);
    }

    private SendOrPostCallback _handleInputCore;
    
    private void HandleInput(RawInputEventArgs e)
    {
        if (PlatformImpl != null)
        {
            Dispatcher.UIThread.Send(_handleInputCore, e);
        }
        else
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this,
                "PlatformImpl is null, couldn't handle input.");
        }
    }
    
    
    private static IInputElement? FirstEnabledAncestor(IInputElement? hitTestElement)
    {
        var candidate = hitTestElement;
        while (candidate?.IsEffectivelyEnabled == false)
        {
            candidate = (candidate as Visual)?.VisualParent as IInputElement;
        }
        return candidate;
    }
    
    public InputElement FocusRoot { get; }
}