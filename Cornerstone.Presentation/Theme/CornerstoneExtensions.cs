#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation.Theme;

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

	#endregion
}