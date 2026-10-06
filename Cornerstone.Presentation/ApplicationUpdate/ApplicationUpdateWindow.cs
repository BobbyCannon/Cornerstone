#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Runtime;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;

#endregion

namespace Cornerstone.Presentation.ApplicationUpdate;

/// <summary>
/// Progress window for <c> -Update &lt;folder&gt; </c>. The theme presents <see cref="ProductImage" />, <see cref="Status" />, and <see cref="Progress" />.
/// </summary>
public class ApplicationUpdateWindow : Window
{
	#region Fields

	public static readonly StyledProperty<IImage> ProductImageProperty;
	public static readonly StyledProperty<int> ProgressProperty;
	public static readonly StyledProperty<string> StatusProperty;

	private bool _updateStarted;

	#endregion

	#region Constructors

	public ApplicationUpdateWindow()
	{
		_updateStarted = false;
		Width = 600;
		Height = 260;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;

		// The window template applies ContentTemplate only when Content is set.
		Content = new object();
	}

	public ApplicationUpdateWindow(ApplicationUpdateWindowOptions options) : this()
	{
		ApplyOptions(options);
	}

	static ApplicationUpdateWindow()
	{
		ProductImageProperty = PresentationProperty.Register<ApplicationUpdateWindow, IImage>(nameof(ProductImage));
		ProgressProperty = PresentationProperty.Register<ApplicationUpdateWindow, int>(nameof(Progress));
		StatusProperty = PresentationProperty.Register<ApplicationUpdateWindow, string>(nameof(Status));
	}

	#endregion

	#region Properties

	public IImage ProductImage
	{
		get => GetValue(ProductImageProperty);
		set => SetValue(ProductImageProperty, value);
	}

	public int Progress
	{
		get => GetValue(ProgressProperty);
		set => SetValue(ProgressProperty, value);
	}

	public string Status
	{
		get => GetValue(StatusProperty);
		set => SetValue(StatusProperty, value);
	}

	// Window.StyleKeyOverride is typeof(Window), which hides the theme keyed by this type.
	protected override Type StyleKeyOverride => typeof(ApplicationUpdateWindow);

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (Design.IsDesignMode)
		{
			Progress = 25;
			Status = "waiting for others to shutdown";
		}
		else if (!_updateStarted)
		{
			_updateStarted = true;
			StartUpdate();
		}

		base.OnLoaded(e);
	}

	private void ApplyOptions(ApplicationUpdateWindowOptions options)
	{
		if (options == null)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(options.Title))
		{
			Title = options.Title;
		}

		var bitmap = LoadBitmap(options.ImageAssembly, options.ProductImage);
		if (bitmap != null)
		{
			ProductImage = bitmap;
		}

		var icon = LoadBitmap(options.ImageAssembly, options.WindowIcon);
		if (icon != null)
		{
			Icon = new WindowIcon(icon);
			TitleBarIcon = icon;
		}
	}

	private static Bitmap LoadBitmap(Assembly assembly, string resourcePath)
	{
		if ((assembly == null) || string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}

		var assemblyName = assembly.GetName().Name;
		if (string.IsNullOrWhiteSpace(assemblyName))
		{
			return null;
		}

		var path = resourcePath.Trim();
		if (!path.StartsWith('/'))
		{
			path = "/" + path;
		}

		var uri = new Uri($"csres://{assemblyName}{path}");
		if (!AssetLoader.Exists(uri))
		{
			return null;
		}

		using var stream = AssetLoader.Open(uri);
		return new Bitmap(stream);
	}

	private void Report(int percent, string status)
	{
		Dispatcher.UIThread.Post(() =>
		{
			Progress = percent;
			Status = status;
		});
	}

	private void StartUpdate()
	{
		var arguments = AppBootstrap.ApplicationArguments;
		var destination = arguments == null ? null : arguments[ApplicationUpdate.ArgumentName];
		if (string.IsNullOrWhiteSpace(destination))
		{
			Progress = 100;
			Status = ApplicationUpdate.GetStatus(ApplicationUpdateResult.DestinationMissing);
			return;
		}

		if (ApplicationUpdate.IsProtectedDestination(destination))
		{
			Progress = 100;
			Status = ApplicationUpdate.GetStatus(ApplicationUpdateResult.ProtectedDestination);
			return;
		}

		var waitForDebugger = arguments.Exists(ApplicationUpdate.DebuggerArgumentName);
		var delay = Debugger.IsAttached ? 250 : 0;
		var source = AppDomain.CurrentDomain.BaseDirectory;
		var applicationFileName = Path.GetFileName(Environment.ProcessPath);
		var progress = new DispatcherProgress(this);

		Task.Run(() =>
		{
			try
			{
				Report(1, "starting");
				Thread.Sleep(delay);

				if (waitForDebugger)
				{
					Report(1, "waiting for the debugger");
					while (!Debugger.IsAttached)
					{
						Thread.Sleep(250);
					}
				}

				Report(25, "waiting for others to shutdown");
				ApplicationUpdate.ShutdownDestinationProcesses(destination);
				Thread.Sleep(delay);

				var result = ApplicationUpdate.TryApply(source, destination, applicationFileName, progress);
				Thread.Sleep(delay);
				if (result == ApplicationUpdateResult.Success)
				{
					Dispatcher.UIThread.Post(() => Close());
				}
			}
			catch (Exception ex)
			{
				Report(100, ex.Message);
			}
		});
	}

	#endregion

	#region Classes

	private sealed class DispatcherProgress : IProgress<ApplicationUpdateProgress>
	{
		#region Fields

		private readonly ApplicationUpdateWindow _window;

		#endregion

		#region Constructors

		public DispatcherProgress(ApplicationUpdateWindow window)
		{
			_window = window;
		}

		#endregion

		#region Methods

		public void Report(ApplicationUpdateProgress value)
		{
			if (value == null)
			{
				return;
			}

			_window.Report(value.Percent, value.Status);
		}

		#endregion
	}

	#endregion
}