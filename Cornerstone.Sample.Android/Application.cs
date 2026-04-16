#region References

using Android.App;
using Android.Runtime;
using Avalonia.Android;

#endregion

namespace Cornerstone.Sample.Android;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
	#region Constructors

	protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
	}

	#endregion
}