#region References

using System;
using System.Diagnostics;
using System.IO;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf.Host;

/// <summary>
/// Starts a Cornerstone desktop exe with --remote tcp-bson://127.0.0.1:port/.
/// </summary>
internal sealed class RemoteAppLauncher : IDisposable
{
	#region Fields

	private Process _process;

	#endregion

	#region Constructors

	public RemoteAppLauncher()
	{
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		Stop();
	}

	public void Start(string appPath, int port)
	{
		Stop();
		var exe = ResolveExecutable(appPath);
		if (!File.Exists(exe))
		{
			throw new FileNotFoundException("Target app was not built.", exe);
		}

		var directory = Path.GetDirectoryName(exe);
		var info = new ProcessStartInfo
		{
			Arguments = "--remote tcp-bson://127.0.0.1:" + port + "/",
			CreateNoWindow = true,
			FileName = exe,
			WorkingDirectory = directory,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			UseShellExecute = false
		};
		var process = Process.Start(info);
		if (process == null)
		{
			throw new InvalidOperationException("The remote app did not start.");
		}

		process.EnableRaisingEvents = true;
		process.OutputDataReceived += OnData;
		process.ErrorDataReceived += OnData;
		process.Exited += OnExited;
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		_process = process;
	}

	public void Stop()
	{
		var process = _process;
		_process = null;
		if (process == null)
		{
			return;
		}

		process.OutputDataReceived -= OnData;
		process.ErrorDataReceived -= OnData;
		process.Exited -= OnExited;
		try
		{
			if (!process.HasExited)
			{
				process.Kill();
			}
		}
		catch (InvalidOperationException)
		{
		}

		process.Dispose();
	}

	private static string ResolveExecutable(string path)
	{
		if (path == null)
		{
			return null;
		}

		if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
		{
			var exe = Path.ChangeExtension(path, ".exe");
			if (File.Exists(exe))
			{
				return exe;
			}
		}

		return path;
	}

	private void OnData(object sender, DataReceivedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(e.Data))
		{
			Output?.Invoke(this, e.Data);
		}
	}

	private void OnExited(object sender, EventArgs e)
	{
		var process = sender as Process;
		var code = 0;
		try
		{
			code = process != null ? process.ExitCode : 0;
		}
		catch (InvalidOperationException)
		{
		}

		Exited?.Invoke(this, code);
	}

	#endregion

	#region Events

	public event EventHandler<int> Exited;

	public event EventHandler<string> Output;

	#endregion
}
