#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Ties editor-host and previewer processes to this Visual Studio process.
/// Windows kills them when Visual Studio exits, including when shutdown skips
/// managed dispose, so an upgrade can delete the previous extension folder.
/// </summary>
internal static class ExtensionProcessLifetime
{
	#region Fields

	private const int JobObjectExtendedLimitInformationClass = 9;
	private const uint JobObjectLimitKillOnJobClose = 0x2000;

	private static readonly object Gate;
	private static IntPtr _job;
	private static Dictionary<int, IntPtr> _killJobs;

	#endregion

	#region Constructors

	static ExtensionProcessLifetime()
	{
		Gate = new object();
		_job = IntPtr.Zero;
		_killJobs = new Dictionary<int, IntPtr>();
	}

	#endregion

	#region Methods

	public static void Track(Process process)
	{
		if (process == null)
		{
			return;
		}

		try
		{
			if (HasExited(process))
			{
				return;
			}

			var job = EnsureJob();
			if ((job != IntPtr.Zero) && !AssignProcessToJobObject(job, process.Handle))
			{
				Log.Debug(
					"Could not tie process {Pid} to Visual Studio lifetime (Win32 {Error})",
					process.Id,
					Marshal.GetLastWin32Error());
			}

			AttachKillJob(process);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Could not tie a launched process to Visual Studio lifetime");
		}
	}

	/// <summary>
	/// Terminates <paramref name="process"/> and any process it has started.
	/// Closing the per-process job is what kills the tree. <see cref="Process.Kill"/>
	/// on net472 leaves those children running until Visual Studio itself exits.
	/// </summary>
	public static void Kill(Process process)
	{
		if (process == null)
		{
			return;
		}

		var id = 0;
		try
		{
			id = process.Id;
		}
		catch (InvalidOperationException)
		{
			id = 0;
		}

		var job = IntPtr.Zero;
		if (id > 0)
		{
			lock (Gate)
			{
				if (_killJobs.TryGetValue(id, out job))
				{
					_killJobs.Remove(id);
				}
			}
		}

		if (job != IntPtr.Zero)
		{
			CloseHandle(job);
			return;
		}

		try
		{
			if (!HasExited(process))
			{
				process.Kill();
			}
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Failed to kill process {Pid}", id);
		}
	}

	private static void AttachKillJob(Process process)
	{
		if (HasExited(process))
		{
			return;
		}

		int id;
		try
		{
			id = process.Id;
		}
		catch (InvalidOperationException)
		{
			return;
		}

		if (id <= 0)
		{
			return;
		}

		var job = CreateJobObject(IntPtr.Zero, null);
		if (job == IntPtr.Zero)
		{
			return;
		}

		var info = new JobObjectExtendedLimitInformation();
		info.BasicLimitInformation.LimitFlags = JobObjectLimitKillOnJobClose;
		var size = Marshal.SizeOf(typeof(JobObjectExtendedLimitInformation));
		if (!SetInformationJobObject(job, JobObjectExtendedLimitInformationClass, ref info, (uint) size) ||
			!AssignProcessToJobObject(job, process.Handle))
		{
			Log.Debug(
				"Could not nest a kill job for process {Pid} (Win32 {Error})",
				id,
				Marshal.GetLastWin32Error());
			CloseHandle(job);
			return;
		}

		lock (Gate)
		{
			IntPtr existing;
			if (_killJobs.TryGetValue(id, out existing) && (existing != IntPtr.Zero))
			{
				CloseHandle(existing);
			}

			_killJobs[id] = job;
		}
	}

	private static IntPtr EnsureJob()
	{
		lock (Gate)
		{
			if (_job != IntPtr.Zero)
			{
				return _job;
			}

			var job = CreateJobObject(IntPtr.Zero, null);
			if (job == IntPtr.Zero)
			{
				Log.Debug("CreateJobObject failed (Win32 {Error})", Marshal.GetLastWin32Error());
				return IntPtr.Zero;
			}

			var info = new JobObjectExtendedLimitInformation();
			info.BasicLimitInformation.LimitFlags = JobObjectLimitKillOnJobClose;
			var size = Marshal.SizeOf(typeof(JobObjectExtendedLimitInformation));
			if (!SetInformationJobObject(job, JobObjectExtendedLimitInformationClass, ref info, (uint) size))
			{
				Log.Debug("SetInformationJobObject failed (Win32 {Error})", Marshal.GetLastWin32Error());
				CloseHandle(job);
				return IntPtr.Zero;
			}

			_job = job;
			return _job;
		}
	}

	private static bool HasExited(Process process)
	{
		try
		{
			return process.HasExited;
		}
		catch (InvalidOperationException)
		{
			return true;
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern IntPtr CreateJobObject(IntPtr jobAttributes, string name);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetInformationJobObject(
		IntPtr job,
		int infoClass,
		ref JobObjectExtendedLimitInformation info,
		uint infoLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr handle);

	[StructLayout(LayoutKind.Sequential)]
	private struct JobObjectBasicLimitInformation
	{
		public long PerProcessUserTimeLimit;
		public long PerJobUserTimeLimit;
		public uint LimitFlags;
		public UIntPtr MinimumWorkingSetSize;
		public UIntPtr MaximumWorkingSetSize;
		public uint ActiveProcessLimit;
		public IntPtr Affinity;
		public uint PriorityClass;
		public uint SchedulingClass;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct IoCounters
	{
		public ulong ReadOperationCount;
		public ulong WriteOperationCount;
		public ulong OtherOperationCount;
		public ulong ReadTransferCount;
		public ulong WriteTransferCount;
		public ulong OtherTransferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JobObjectExtendedLimitInformation
	{
		public JobObjectBasicLimitInformation BasicLimitInformation;
		public IoCounters IoInfo;
		public UIntPtr ProcessMemoryLimit;
		public UIntPtr JobMemoryLimit;
		public UIntPtr PeakProcessMemoryUsed;
		public UIntPtr PeakJobMemoryUsed;
	}

	#endregion
}
