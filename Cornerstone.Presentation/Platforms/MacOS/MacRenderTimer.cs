using System;
using System.Diagnostics;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Rendering;

namespace Cornerstone.Presentation.Platforms.MacOS;

internal sealed class MacRenderTimer : NativeCallbackBase, IRenderTimer, ICsnActionCallback
{
    private readonly ICsnPlatformRenderTimer _platformRenderTimer;
    private readonly Stopwatch _stopwatch;
    private volatile Action<TimeSpan>? _tick;
    private bool _registered;

    public MacRenderTimer(ICsnPlatformRenderTimer platformRenderTimer)
    {
        _platformRenderTimer = platformRenderTimer;
        _stopwatch = Stopwatch.StartNew();
    }

    public Action<TimeSpan>? Tick
    {
        get => _tick;
        set
        {
            if (value != null)
            {
                _tick = value;
                EnsureRegistered();
                _platformRenderTimer.Start();
            }
            else
            {
                _platformRenderTimer.Stop();
                _tick = null;
            }
        }
    }

    public bool RunsInBackground => _platformRenderTimer.RunsInBackground().FromComBool();

    private void EnsureRegistered()
    {
        if (!_registered)
        {
            _registered = true;
            var registrationResult = _platformRenderTimer.RegisterTick(this);
            if (registrationResult != 0)
            {
                throw new InvalidOperationException(
                    $"Cornerstone.Presentation.Platforms.MacOS was not able to start the RenderTimer. Native error code is: {registrationResult}");
            }
        }
    }

    public void Run()
    {
        _tick?.Invoke(_stopwatch.Elapsed);
    }
}
