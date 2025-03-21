#region References

using System;
using System.Diagnostics;
using System.Text;

#endregion

namespace Cornerstone.Runtime;

public class ProcessRunner
{
	#region Constructors

	public ProcessRunner()
	{
		Cancel = null;
		Details = new ProcessDetails();
		RaiseError = true;
		TreatErrorAsOutput = false;
	}

	#endregion

	#region Properties

	public CancelToken Cancel { get; set; }

	public ProcessDetails Details { get; set; }

	public bool RaiseError { get; set; }

	public bool TreatErrorAsOutput { get; set; }

	#endregion

	#region Methods

	public ReadToEndResult ReadToEnd()
	{
		var start = GetProcessStartInfo();
		using var process = new Process();
		process.StartInfo = start;

		ReadToEndResult response;

		try
		{
			try
			{
				Details.StartedOn = DateTimeProvider.RealTime.UtcNow;

				process.Start();
			}
			catch (Exception e)
			{
				return new ReadToEndResult
				{
					IsSuccess = false,
					StdOut = string.Empty,
					StdErr = e.Message
				};
			}

			response = new ReadToEndResult
			{
				StdOut = process.StandardOutput.ReadToEnd(),
				StdErr = process.StandardError.ReadToEnd()
			};

			process.WaitForExit();
			response.IsSuccess = process.ExitCode == 0;
			process.Close();
		}
		finally
		{
			Details.StoppedOn = DateTimeProvider.RealTime.UtcNow;
		}

		return response;
	}

	public static bool Run(ProcessDetails details)
	{
		var runner = new ProcessRunner();
		runner.Details.UpdateWith(details);
		return runner.Run();
	}

	public virtual bool Run()
	{
		var start = GetProcessStartInfo();
		using var process = new Process();
		process.StartInfo = start;

		try
		{
			Details.StartedOn = DateTimeProvider.RealTime.UtcNow;

			process.OutputDataReceived += OnProcOnOutputDataReceived;
			process.ErrorDataReceived += OnProcessOnErrorDataReceived;

			try
			{
				process.Start();
				Details.ProcessId = process.Id;
			}
			catch (Exception ex)
			{
				Details.Exception = ex;
				return false;
			}

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();

			var exitCode = process.ExitCode;
			process.Close();

			return Details.WasCancelled
				|| (exitCode == 0)
				|| (Details.Errors.Count <= 0);
		}
		finally
		{
			Details.StoppedOn = DateTimeProvider.RealTime.UtcNow;

			process.OutputDataReceived -= OnProcOnOutputDataReceived;
			process.ErrorDataReceived -= OnProcessOnErrorDataReceived;
		}
	}

	protected virtual bool CheckAsError(string data)
	{
		return true;
	}

	protected virtual ProcessStartInfo GetProcessStartInfo()
	{
		var response = new ProcessStartInfo
		{
			FileName = Details.FilePath,
			Arguments = Details.Arguments,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};

		if (!string.IsNullOrEmpty(Details.WorkingDirectory))
		{
			response.WorkingDirectory = Details.WorkingDirectory;
		}

		return response;
	}

	protected virtual void OnReadline(string line)
	{
	}

	private void HandleNotExitedProcess(Process process)
	{
		if (!process.HasExited)
		{
			#if (NETSTANDARD2_0)
			process.Kill();
			#else
			process.Kill(true);
			#endif
		}
	}

	private void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
	{
		var process = (Process) sender;

		if (Cancel is { Requested: true })
		{
			Details.WasCancelled = true;
			process.CancelErrorRead();
			process.CancelOutputRead();
			HandleNotExitedProcess(process);
			return;
		}

		if (string.IsNullOrEmpty(e.Data))
		{
			return;
		}

		if (TreatErrorAsOutput)
		{
			OnReadline(e.Data);
		}
		else if (CheckAsError(e.Data))
		{
			Details.Errors.Add(e.Data);
		}
	}

	private void OnProcOnOutputDataReceived(object sender, DataReceivedEventArgs e)
	{
		var process = (Process) sender;

		if (Cancel is { Requested: true })
		{
			Details.WasCancelled = true;
			process.CancelErrorRead();
			process.CancelOutputRead();
			HandleNotExitedProcess(process);
			return;
		}

		if (e.Data != null)
		{
			OnReadline(e.Data);
		}
	}

	#endregion

	#region Classes

	public class CancelToken
	{
		#region Properties

		public bool Requested { get; set; } = false;

		#endregion
	}

	public class ReadToEndResult
	{
		#region Properties

		public bool IsSuccess { get; set; }

		public string StdErr { get; set; }

		public string StdOut { get; set; }

		#endregion
	}

	#endregion
}