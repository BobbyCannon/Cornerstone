using System;
using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Cornerstone.Presentation.Android;

public interface IActivityResultHandler
{
    public Action<int, Result, Intent?>? ActivityResult { get; set; }
        
    public Action<int, string[], Permission[]>? RequestPermissionsResult { get; set; }
}
