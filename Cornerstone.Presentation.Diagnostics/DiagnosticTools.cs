#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Diagnostics.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.Presentation.Diagnostics;

public static class DiagnosticTools
{
	#region Fields

	private static readonly Dictionary<IDevToolsTopLevelGroup, AppWindow> _open;

	#endregion

	#region Constructors

	static DiagnosticTools()
	{
		_open = new();
	}

	#endregion

	#region Methods

	public static IDisposable Attach(TopLevel root, KeyGesture gesture)
	{
		return Attach(root, new DiagnosticToolsOptions { Gesture = gesture });
	}

	public static IDisposable Attach(TopLevel root, DiagnosticToolsOptions options)
	{
		void PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (options.Gesture.Matches(e))
			{
				Open(root, options);
			}
		}

		return (root ?? throw new ArgumentNullException(nameof(root)))
			.AddDisposableHandler(
				InputElement.KeyDownEvent,
				PreviewKeyDown,
				RoutingStrategies.Tunnel
			);
	}

	public static IDisposable Attach(Application application, DiagnosticToolsOptions options)
	{
		var result = new CompositeDisposable(2);

		// Skip if call on Design Mode
		if (!Design.IsDesignMode)
		{
			var lifeTime = application.ApplicationLifetime
				as IClassicDesktopStyleApplicationLifetime;

			if (lifeTime is null)
			{
				throw new ArgumentNullException(nameof(application), "DiagnosticTools can only attach to applications that support IClassicDesktopStyleApplicationLifetime.");
			}

			if (application.InputManager is not null)
			{
				result.Add(application.InputManager.PreProcess.Subscribe(e =>
				{
					var owner = lifeTime.MainWindow;

					if (e is RawKeyEventArgs keyEventArgs
						&& (keyEventArgs.Type == RawKeyEventType.KeyUp)
						&& options.Gesture.Matches(keyEventArgs))
					{
						result.Add(Open(new ClassicDesktopStyleApplicationLifetimeTopLevelGroup(lifeTime), options, owner, application));
						e.Handled = true;
					}
				}));
			}
		}
		return result;
	}

	public static bool DoesBelongToDevTool(this Visual v)
	{
		var topLevel = TopLevel.GetTopLevel(v);

		while (topLevel is not null && topLevel is not AppWindow)
		{
			if (topLevel is PopupRoot pr)
			{
				topLevel = pr.ParentTopLevel;
			}
			else
			{
				return false;
			}
		}
		return true;
	}

	public static IDisposable Open(TopLevel root, DiagnosticToolsOptions options)
	{
		return Open(new SingleViewTopLevelGroup(root), options, root as Window, null);
	}

	public static IDisposable Open(IDevToolsTopLevelGroup group, DiagnosticToolsOptions options)
	{
		return Open(group, options, null, null);
	}

	private static void DiagnosticToolsClosed(object sender, EventArgs e)
	{
		var window = (AppWindow) sender!;
		window.Closed -= DiagnosticToolsClosed;
		_open.Remove((IDevToolsTopLevelGroup) window.Tag!);
	}

	private static IDisposable Open(IDevToolsTopLevelGroup topLevelGroup, DiagnosticToolsOptions options,
		Window owner, Application app)
	{
		var focusedControl = owner?.FocusManager?.GetFocusedElement() as Control;
		PresentationObject root = topLevelGroup switch
		{
			ClassicDesktopStyleApplicationLifetimeTopLevelGroup gr => new Controls.Application(gr, app ?? Application.Current!),
			SingleViewTopLevelGroup gr => gr.Items.First(),
			_ => new TopLevelGroup(topLevelGroup)
		};

		// If single static top level is already visible in another devtools window, focus it.
		if (_open.TryGetValue(topLevelGroup, out var mainWindow))
		{
			mainWindow.Activate();
			mainWindow.SelectedControl(focusedControl);
			return Disposable.Empty;
		}
		if ((topLevelGroup.Items.Count == 1) && topLevelGroup.Items is not INotifyCollectionChanged)
		{
			var singleTopLevel = topLevelGroup.Items.First();

			foreach (var group in _open)
			{
				if (group.Key.Items.Contains(singleTopLevel))
				{
					group.Value.Activate();
					group.Value.SelectedControl(focusedControl);
					return Disposable.Empty;
				}
			}
		}

		var window = new AppWindow
		{
			Root = root,
			Width = options.Size.Width,
			Height = options.Size.Height,
			Tag = topLevelGroup
		};
		window.SetOptions(options);
		window.SelectedControl(focusedControl);
		window.Closed += DiagnosticToolsClosed;
		
		_open.Add(topLevelGroup, window);
		
		if (options.ShowAsChildWindow && owner is not null)
		{
			window.Show(owner);
		}
		else
		{
			window.Show();
		}
		return Disposable.Create(() => window.Close());
	}

	#endregion
}