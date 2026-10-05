#region References

using System;
using System.Diagnostics;
using System.Windows;
using Cornerstone.Presentation.Remote.Wpf;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf.Host;

public partial class MainWindow : Window
{
	#region Fields

	private readonly RemoteAppLauncher _launcher;
	private string _lastHostLine;
	private RemoteSession _session;

	#endregion

	#region Constructors

	public MainWindow()
	{
		_launcher = new RemoteAppLauncher();
		_lastHostLine = null;
		InitializeComponent();
		Loaded += OnLoaded;
		Closed += OnClosed;
	}

	#endregion

	#region Methods

	private static void DebugLine(string line)
	{
		Debug.WriteLine(line);
	}

	private void OnClosed(object sender, EventArgs e)
	{
		_launcher.Output -= OnHostOutput;
		_launcher.Exited -= OnHostExited;
		_launcher.Dispose();
		if (_session != null)
		{
			_session.Faulted -= OnSessionFaulted;
			_session.Dispose();
			_session = null;
		}
	}

	private void OnHostExited(object sender, int exitCode)
	{
		Dispatcher.BeginInvoke(new Action(() =>
		{
			if (!string.IsNullOrWhiteSpace(_lastHostLine))
			{
				SetStatus("App exited (" + exitCode + "): " + _lastHostLine);
			}
			else
			{
				SetStatus("App exited (" + exitCode + ").");
			}
		}));
	}

	private void OnHostOutput(object sender, string line)
	{
		_lastHostLine = line;
		DebugLine(line);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Loaded -= OnLoaded;
		try
		{
			_session = new RemoteSession();
			_session.Faulted += OnSessionFaulted;
			RemoteSurface.Session = _session;
			_launcher.Output += OnHostOutput;
			_launcher.Exited += OnHostExited;
			var port = _session.ListenLoopback();
			SetStatus("Listening on " + port + "…");
			_launcher.Start(RemoteHostPaths.AppDll, port);
			SetStatus("Sample.Desktop --remote on port " + port + ".");
		}
		catch (Exception ex)
		{
			SetStatus(ex.Message);
		}
	}

	private void OnSessionFaulted(object sender, Exception exception)
	{
		Dispatcher.BeginInvoke(new Action(() => SetStatus(exception.Message)));
	}

	private void SetStatus(string text)
	{
		StatusText.Text = text;
	}

	#endregion
}
