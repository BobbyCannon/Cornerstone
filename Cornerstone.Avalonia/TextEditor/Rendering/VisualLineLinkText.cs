﻿#region References

using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// VisualLineElement that represents a piece of text and is a clickable link.
/// </summary>
public class VisualLineLinkText : VisualLineText
{
	#region Constructors

	/// <summary>
	/// Creates a visual line text element with the specified length.
	/// It uses the <see cref="ITextRunConstructionContext.VisualLine" /> and its
	/// <see cref="VisualLineElement.RelativeTextOffset" /> to find the actual text string.
	/// </summary>
	public VisualLineLinkText(VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
	{
		RequireControlModifierForClick = true;
	}

	static VisualLineLinkText()
	{
		OpenUriEvent.AddClassHandler<Window>(ExecuteOpenUriEventHandler);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the URL that is navigated to when the link is clicked.
	/// </summary>
	public Uri NavigateUri { get; set; }

	/// <summary>
	/// Routed event that should navigate to uri when the link is clicked.
	/// </summary>
	public static RoutedEvent<OpenUriRoutedEventArgs> OpenUriEvent { get; } = RoutedEvent.Register<VisualLineText, OpenUriRoutedEventArgs>(nameof(OpenUriEvent), RoutingStrategies.Bubble);

	/// <summary>
	/// Gets/Sets whether the user needs to press Control to click the link.
	/// The default value is true.
	/// </summary>
	public bool RequireControlModifierForClick { get; set; }

	/// <summary>
	/// Gets/Sets the window name where the URL will be opened.
	/// </summary>
	public string TargetName { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
		TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
		if (context.TextView.LinkTextUnderline)
		{
			TextRunProperties.SetTextDecorations(TextDecorations.Underline);
		}
		return base.CreateTextRun(startVisualColumn, context);
	}

	/// <inheritdoc />
	protected internal override void OnPointerPressed(PointerPressedEventArgs e)
	{
		if (!e.Handled && LinkIsClickable(e.KeyModifiers))
		{
			var eventArgs = new OpenUriRoutedEventArgs(NavigateUri) { RoutedEvent = OpenUriEvent };

			if (e.Source is Interactive interactive)
			{
				interactive.RaiseEvent(eventArgs);
			}

			e.Handled = true;
		}
	}

	/// <inheritdoc />
	protected internal override void OnQueryCursor(PointerEventArgs e)
	{
		if (LinkIsClickable(e.KeyModifiers))
		{
			if (e.Source is InputElement inputElement)
			{
				inputElement.Cursor = new Cursor(StandardCursorType.Hand);
			}
			e.Handled = true;
		}
	}

	/// <inheritdoc />
	protected override VisualLineText CreateInstance(int length)
	{
		return new VisualLineLinkText(ParentVisualLine, length)
		{
			NavigateUri = NavigateUri,
			TargetName = TargetName,
			RequireControlModifierForClick = RequireControlModifierForClick
		};
	}

	/// <summary>
	/// Gets whether the link is currently clickable.
	/// </summary>
	/// <remarks>
	/// Returns true when control is pressed; or when
	/// <see cref="RequireControlModifierForClick" /> is disabled.
	/// </remarks>
	protected virtual bool LinkIsClickable(KeyModifiers modifiers)
	{
		if (NavigateUri == null)
		{
			return false;
		}
		if (RequireControlModifierForClick)
		{
			return modifiers.HasFlag(KeyModifiers.Control);
		}
		return true;
	}

	private static void ExecuteOpenUriEventHandler(Window window, OpenUriRoutedEventArgs arg)
	{
		var url = arg.Uri.ToString();
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch (Exception)
		{
			// Process.Start can throw several errors (not all of them documented),
			// just ignore all of them.
		}
	}

	#endregion
}

public sealed class OpenUriRoutedEventArgs : RoutedEventArgs
{
	#region Constructors

	public OpenUriRoutedEventArgs(Uri uri)
	{
		Uri = uri ?? throw new ArgumentNullException(nameof(uri));
	}

	#endregion

	#region Properties

	public Uri Uri { get; }

	#endregion
}