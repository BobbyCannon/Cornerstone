#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Reactive;

#endregion

namespace Cornerstone.Avalonia;

/// <summary>
/// This is for extension that I always want available.
/// </summary>
public static class CornerstoneExtensions
{
	#region Methods

	public static Typeface CreateTypeface(this Control control,
		FontWeight? fontWeight = null, FontStyle? fontStyle = null)
	{
		return new Typeface(
			control.GetValue(TextElement.FontFamilyProperty),
			fontStyle ?? control.GetValue(TextElement.FontStyleProperty),
			fontWeight ?? control.GetValue(TextElement.FontWeightProperty),
			control.GetValue(TextElement.FontStretchProperty)
		);
	}

	public static double GetBestSingle(CornerRadius t)
	{
		if (t.IsUniform)
		{
			return t.TopLeft;
		}

		return (t.TopLeft + t.TopRight + t.BottomLeft + t.BottomRight) / 4.0;
	}

	public static double GetBestSingle(Thickness t)
	{
		if (t.IsUniform)
		{
			return t.Left;
		}

		return (t.Left + t.Top + t.Right + t.Bottom) / 4.0;
	}

	public static TopLevel GetTopLevel(this Application app)
	{
		return app?.ApplicationLifetime switch
		{
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow!,
			ISingleViewApplicationLifetime viewApp => TopLevel.GetTopLevel(viewApp.MainView),
			_ => null!
		};
	}

	public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> action)
	{
		return observable.Subscribe(new AnonymousObserver<T>(action));
	}

	public static IBrush WithOpacity(IBrush brush, double opacity)
	{
		return brush switch
		{
			IImmutableSolidColorBrush solid => new SolidColorBrush(solid.Color, opacity),
			SolidColorBrush solid => new SolidColorBrush(solid.Color, opacity),
			_ => brush
		};
	}

	#endregion
}