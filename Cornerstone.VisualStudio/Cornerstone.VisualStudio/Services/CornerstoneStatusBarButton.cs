#region References

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Microsoft.VisualStudio.PlatformUI;
using Cornerstone.VisualStudio.Views;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Adds one Cornerstone button to the shell status bar. The shell has no supported
/// slot for an extension button, so this inserts into the status bar panel.
/// </summary>
internal static class CornerstoneStatusBarButton
{
	#region Fields

	private static Button _button;
	private static RotateTransform _rotation;
	private static bool _spinning;
	private static int _workingQueued;

	#endregion

	#region Constructors

	static CornerstoneStatusBarButton()
	{
		_button = null;
		_rotation = null;
		_spinning = false;
		_workingQueued = 0;
	}

	#endregion

	#region Methods

	public static void ShowProcesses(AsyncPackage package)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var window = package.FindToolWindow(typeof(CornerstoneProcessesWindow), 0, true);
		var frame = window?.Frame as IVsWindowFrame;
		if (frame != null)
		{
			frame.Show();
		}
	}

	public static async Task InjectAsync(AsyncPackage package)
	{
		await package.JoinableTaskFactory.SwitchToMainThreadAsync();
		var panel = await FindPanelAsync(package).ConfigureAwait(true);
		if (panel == null)
		{
			Log.Warning("Cornerstone status bar button was not added; the shell status bar panel was not found.");
			return;
		}

		var rotation = new RotateTransform();
		var icon = new TextBlock();
		icon.Text = "\uE713";
		icon.FontFamily = new FontFamily("Segoe MDL2 Assets");
		icon.FontSize = 14;
		icon.HorizontalAlignment = HorizontalAlignment.Center;
		icon.VerticalAlignment = VerticalAlignment.Center;
		icon.RenderTransform = rotation;
		icon.RenderTransformOrigin = new Point(0.5, 0.5);
		icon.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.StatusBarDefaultTextBrushKey);

		var hover = new Border();
		hover.Background = Brushes.Transparent;
		hover.Child = icon;

		var button = new Button();
		button.Content = hover;
		button.ToolTip = EditorHostSession.HostToolTip();
		button.Width = 32;
		button.Padding = new Thickness(0);
		button.Margin = new Thickness(0);
		button.Background = Brushes.Transparent;
		button.BorderThickness = new Thickness(0);
		button.Focusable = false;
		button.Template = FlatTemplate();
		button.SetResourceReference(Control.ForegroundProperty, EnvironmentColors.StatusBarDefaultTextBrushKey);
		button.MouseEnter += (sender, args) => hover.Background = HoverWash(icon);
		button.MouseLeave += (sender, args) => hover.Background = Brushes.Transparent;
		button.Click += (sender, args) => ShowProcesses(package);
		DockPanel.SetDock(button, Dock.Right);
		panel.Children.Insert(IndexBeforeResizeGrip(panel), button);
		_button = button;
		_rotation = rotation;
		EditorHostActivity.Changed += OnWorkingChanged;
		ApplyWorking(EditorHostActivity.IsWorking);
	}

	private static void OnWorkingChanged()
	{
		if (System.Threading.Interlocked.Exchange(ref _workingQueued, 1) == 1)
		{
			return;
		}

		var button = _button;
		var dispatcher = button != null ? button.Dispatcher : Application.Current?.Dispatcher;
		if ((dispatcher == null) || dispatcher.HasShutdownStarted)
		{
			System.Threading.Interlocked.Exchange(ref _workingQueued, 0);
			return;
		}

		// Same priority rule as preview frames. A Normal post per assembly sits
		// above the keyboard for the whole metadata load.
		#pragma warning disable VSTHRD001
		dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyWorkingOnUi));
		#pragma warning restore VSTHRD001
	}

	private static void ApplyWorkingOnUi()
	{
		try
		{
			ApplyWorking(EditorHostActivity.IsWorking);
		}
		finally
		{
			System.Threading.Interlocked.Exchange(ref _workingQueued, 0);
		}
	}

	internal static void RefreshToolTip()
	{
		if (_button == null)
		{
			return;
		}

		_button.ToolTip = EditorHostSession.HostToolTip();
	}

	private static void ApplyWorking(bool working)
	{
		if ((_rotation == null) || (_button == null))
		{
			return;
		}

		_button.ToolTip = EditorHostSession.HostToolTip();
		if (working)
		{
			if (_spinning)
			{
				return;
			}

			_spinning = true;
			var animation = new DoubleAnimation();
			animation.From = 0;
			animation.To = 360;
			animation.Duration = new Duration(TimeSpan.FromSeconds(3));
			animation.RepeatBehavior = RepeatBehavior.Forever;
			_rotation.BeginAnimation(RotateTransform.AngleProperty, animation);
			return;
		}

		_spinning = false;
		_rotation.BeginAnimation(RotateTransform.AngleProperty, null);
		_rotation.Angle = 0;
	}

	private static int IndexBeforeResizeGrip(DockPanel panel)
	{
		for (var i = 0; i < panel.Children.Count; i++)
		{
			var child = panel.Children[i] as FrameworkElement;
			if (child == null)
			{
				continue;
			}

			if ((child is System.Windows.Controls.Primitives.ResizeGrip) ||
				((child.Name != null) && (child.Name.IndexOf("Resize", StringComparison.OrdinalIgnoreCase) >= 0)))
			{
				return i + 1;
			}
		}

		return panel.Children.Count;
	}

	private static ControlTemplate FlatTemplate()
	{
		var border = new FrameworkElementFactory(typeof(Border));
		border.Name = "Background";
		border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
		var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(presenter);

		var template = new ControlTemplate(typeof(Button));
		template.VisualTree = border;
		return template;
	}

	private static SolidColorBrush HoverWash(TextBlock icon)
	{
		var color = Colors.White;
		var brush = icon.Foreground as SolidColorBrush;
		if (brush != null)
		{
			color = brush.Color;
		}

		return new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B));
	}

	private static async Task<DockPanel> FindPanelAsync(AsyncPackage package)
	{
		for (var attempt = 0; attempt < 12; attempt++)
		{
			var root = MainWindowRoot(package);
			var panel = FindNamed(root, "StatusBarPanel") as DockPanel;
			if (panel == null)
			{
				panel = FindStatusBar(root);
			}

			if (panel != null)
			{
				return panel;
			}

			await Task.Delay(1000).ConfigureAwait(true);
		}

		return null;
	}

	private static DependencyObject MainWindowRoot(AsyncPackage package)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
		if ((dte != null) && (dte.MainWindow != null))
		{
			var source = System.Windows.Interop.HwndSource.FromHwnd(dte.MainWindow.HWnd);
			if ((source != null) && (source.RootVisual != null))
			{
				return source.RootVisual;
			}
		}

		return Application.Current == null ? null : Application.Current.MainWindow;
	}

	private static DockPanel FindStatusBar(DependencyObject parent)
	{
		if (parent == null)
		{
			return null;
		}

		var count = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			var panel = child as DockPanel;
			var element = child as FrameworkElement;
			if ((panel != null) && (element.Name != null) && element.Name.IndexOf("StatusBar", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return panel;
			}

			var nested = FindStatusBar(child);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	private static DependencyObject FindNamed(DependencyObject parent, string childName)
	{
		if (parent == null)
		{
			return null;
		}

		var count = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			var element = child as FrameworkElement;
			if ((element != null) && (element.Name == childName))
			{
				return element;
			}

			var nested = FindNamed(child, childName);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	#endregion
}
