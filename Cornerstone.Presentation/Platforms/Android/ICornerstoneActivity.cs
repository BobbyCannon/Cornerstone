using System;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;

namespace Cornerstone.Presentation.Android;

public interface ICornerstoneActivity : IActivityResultHandler, IActivityNavigationService
{
    object? Content { get; set; }
    event EventHandler<ActivatedEventArgs>? Activated;
    event EventHandler<ActivatedEventArgs>? Deactivated;
}
