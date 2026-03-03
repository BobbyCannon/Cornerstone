#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
#if (NET8_0_OR_GREATER)
using System.Runtime.CompilerServices;
#endif

#pragma warning disable RS1035

#endregion

namespace Cornerstone.Generators;

internal class TestRunner
{
	#region Fields

	private readonly string[] _args;

	#endregion

	#region Constructors

	public TestRunner(params string[] args)
	{
		_args = args;

		Classes = [];
		Filter = null;
		Verbose = false;

		ParseArgs(args);
	}

	#endregion

	#region Properties

	public List<TestClassInfo> Classes { get; }

	public string[] Filter { get; private set; }

	public bool Verbose { get; private set; }

	#endregion

	#region Methods

	public void AddTest(TestClassInfo testClass)
	{
		Classes.Add(testClass);
	}

	public void Process()
	{
		#if NET8_0_OR_GREATER
		var isAot = !RuntimeFeature.IsDynamicCodeSupported;
		#else
		var isAot = false; // or leave dynamic code path always on
		#endif

		Console.WriteLine(
			"""
			[92m
			 ██████╗ ██████╗ ██████╗ ███╗   ██╗███████╗██████╗ ███████╗████████╗ ██████╗ ███╗   ██╗███████╗
			██╔════╝██╔═══██╗██╔══██╗████╗  ██║██╔════╝██╔══██╗██╔════╝╚══██╔══╝██╔═══██╗████╗  ██║██╔════╝
			██║     ██║   ██║██████╔╝██╔██╗ ██║█████╗  ██████╔╝███████╗   ██║   ██║   ██║██╔██╗ ██║█████╗  
			██║     ██║   ██║██╔══██╗██║╚██╗██║██╔══╝  ██╔══██╗╚════██║   ██║   ██║   ██║██║╚██╗██║██╔══╝  
			╚██████╗╚██████╔╝██║  ██║██║ ╚████║███████╗██║  ██║███████║   ██║   ╚██████╔╝██║ ╚████║███████╗
			 ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝    ╚═════╝ ╚═╝  ╚═══╝╚══════╝
			[0m
			""");

		Console.WriteLine(
			$"""
			[37m       Args: [0m{string.Join(", ", _args)}
			[37m    Bitness: [0m{(Environment.Is64BitProcess ? "x64" : "x86")}
			[37m    Machine: [0m{Environment.MachineName}
			""");

		if (GetPhysicallyInstalledSystemMemory(out var memory))
		{
			Console.WriteLine($"\e[37m     Memory: \e[0m{memory / 1024 / 1024} GB");
		}

		Console.WriteLine(
			$"""
			[37m     Native: [0m{isAot}
			[37m     Filter: [0m{Filter}
			[37m    Verbose: [0m{Verbose}
			""");

		Console.WriteLine();

		var totalPassed = 0;
		var totalFailed = 0;
		var originalOut = Console.Out;
		var totalWatch = Stopwatch.StartNew();

		foreach (var testClass in Classes)
		{
			if (Filter is { Length: >= 1 }
				&& (testClass.ClassName != Filter[0]))
			{
				continue;
			}

			var builder = new StringBuilder(16384);
			using var stringWriter = new StringWriter(builder);

			foreach (var method in testClass.TestMethods)
			{
				if (Filter is { Length: >= 2 }
					&& (method.Name != Filter[1]))
				{
					continue;
				}

				builder.Clear();
				
				var testWatch = Stopwatch.StartNew();
				Console.SetOut(stringWriter);

				try
				{
					var instance = testClass.ConstructorInfo.Invoke([]);
					if (instance == null)
					{
						Console.SetOut(originalOut);
						Console.WriteLine($"Inconclusive: {testClass.ClassName} could not create class instance.");
						break;
					}

					testClass.InitializeMethod?.MethodInfo.Invoke(instance, []);

					var methodInfo = method.MethodInfo;
					if (methodInfo == null)
					{
						Console.SetOut(originalOut);
						Console.WriteLine($"Inconclusive: {testClass.ClassName}.{method.Name} could not get method info...");
						continue;
					}

					methodInfo.Invoke(instance, []);
					testClass.CleanupMethod?.MethodInfo.Invoke(instance, []);
					testWatch.Stop();
					totalPassed++;
					Console.SetOut(originalOut);
					Console.WriteLine($"{$"{testWatch.Elapsed.TotalMilliseconds:F4} ms",12} \e[92mPassed\e[0m: {testClass.ClassName}.{method.Name}");
					if (Verbose && (builder.Length > 0))
					{
						Console.WriteLine($"\e[96m{builder}\e[0m");
					}
				}
				catch (Exception ex)
				{
					totalFailed++;
					Console.SetOut(originalOut);
					Console.WriteLine($"{$"{testWatch.Elapsed.TotalMilliseconds:F4} ms",12} \e[91mFailed\e[0m: {testClass.ClassName}.{method.Name}");
					Console.WriteLine($"\t\e[97;41m{ex}\e[0m");
					if (Verbose && (builder.Length > 0))
					{
						Console.WriteLine($"\e[96m{builder}\e[0m");
					}
				}
			}
		}

		Console.WriteLine();
		Console.Write($"{$"{totalWatch.Elapsed.TotalMilliseconds:F4} ms",12} Elapsed, {totalPassed} Passed,");
		Console.WriteLine(totalFailed == 0 ? $" {totalFailed} Failed" : $"\e[97;41m {totalFailed} Failed \e[0m");
		Console.WriteLine();
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

	private void ParseArgs(string[] args)
	{
		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i].ToLowerInvariant();

			switch (arg)
			{
				case "-v" or "--verbose":
				{
					Verbose = true;
					break;
				}
				case "-f" or "--filter":
				{
					// Look at next argument if it exists
					if ((i + 1) < args.Length)
					{
						Filter = args[i + 1].Split(["."], StringSplitOptions.RemoveEmptyEntries);
						i++;
					}
					break;
				}
			}
		}
	}

	#endregion
}

public class TestClassInfo
{
	#region Properties

	public string ClassName { get; init; }
	public TestMethodInfo CleanupMethod { get; init; }
	public ConstructorInfo ConstructorInfo { get; init; }
	public TestMethodInfo InitializeMethod { get; init; }
	public TestMethodInfo[] TestMethods { get; init; }

	#endregion
}

public class TestMethodInfo
{
	#region Properties

	public MethodInfo MethodInfo { get; init; }
	public string Name { get; init; }

	#endregion
}