#region References
#if ANDROID
using Android.App;
using Android.Content.Res;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
#endif
#if WINDOWS
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
#endif
#endregion

namespace Cornerstone.Maui;

/// <summary>
/// Maui platform specific implementations.
/// </summary>
public static class MauiPlatform
{
	#region Constructors

	static MauiPlatform()
	{
		#if WINDOWS
		PickerHandler.Mapper.Add(nameof(View.HorizontalOptions), MapHorizontalOptions);
		#endif
	}

	#endregion

	#region Properties

	#if ANDROID

	/// <summary>
	/// The main activity for Android platform.
	/// </summary>
	public static Activity MainActivity { get; private set; }

	#endif

	#endregion

	#region Methods

	#if ANDROID

	/// <summary>
	/// Initialize the Maui platform for Android.
	/// </summary>
	/// <param name="activity"> The activity. </param>
	public static void Initialize(Activity activity)
	{
		MainActivity = activity;

		EntryHandler.Mapper.AppendToMapping(nameof(Entry),
			(handler, view) =>
			{
				handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
			}
		);
	}

	#endif

	#if WINDOWS

	/// <summary>
	/// Initialize the Maui platform for Windows.
	/// </summary>
	public static void Initialize()
	{
	}

	private static void MapHorizontalOptions(IViewHandler handler, IView view)
	{
		if (view is not View mauiView)
		{
			return;
		}

		if (handler.PlatformView is not Microsoft.UI.Xaml.FrameworkElement element)
		{
			return;
		}

		element.HorizontalAlignment = mauiView.HorizontalOptions.Alignment switch
		{
			LayoutAlignment.Start => Microsoft.UI.Xaml.HorizontalAlignment.Left,
			LayoutAlignment.Center => Microsoft.UI.Xaml.HorizontalAlignment.Center,
			LayoutAlignment.End => Microsoft.UI.Xaml.HorizontalAlignment.Right,
			LayoutAlignment.Fill => Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
			_ => Microsoft.UI.Xaml.HorizontalAlignment.Left
		};
	}

	#endif

	#endregion
}