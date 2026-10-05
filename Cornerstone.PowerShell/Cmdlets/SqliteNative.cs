#region References

using System;
using System.IO;
using System.Runtime.InteropServices;
using SQLitePCL;

#endregion

namespace Cornerstone.PowerShell.Cmdlets;

internal static class SqliteNative
{
	#region Fields

	private static bool _initialized;

	#endregion

	#region Constructors

	static SqliteNative()
	{
		_initialized = false;
	}

	#endregion

	#region Methods

	public static void Init()
	{
		if (_initialized)
		{
			return;
		}

		var moduleDir = Path.GetDirectoryName(typeof(SqliteNative).Assembly.Location);
		if (!string.IsNullOrWhiteSpace(moduleDir))
		{
			var rid = RuntimeInformation.ProcessArchitecture switch
			{
				Architecture.X64 => "win-x64",
				Architecture.X86 => "win-x86",
				Architecture.Arm64 => "win-arm64",
				_ => null
			};
			if (rid != null)
			{
				var native = Path.Combine(moduleDir, "runtimes", rid, "native");
				if (Directory.Exists(native))
				{
					var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
					Environment.SetEnvironmentVariable("PATH", native + Path.PathSeparator + path);
				}
			}
		}

		Batteries.Init();
		_initialized = true;
	}

	#endregion
}
