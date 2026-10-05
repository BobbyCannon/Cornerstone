#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

#pragma warning disable RS1035

#endregion

namespace Cornerstone.Generators;

public class TestRunner
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
		Quiet = false;
		Verbose = false;

		ParseArgs(args);
	}

	#endregion

	#region Properties

	public List<TestClassInfo> Classes { get; }

	public string[] Filter { get; private set; }

	public bool Quiet { get; private set; }

	public bool Verbose { get; private set; }

	#endregion

	#region Methods

	public void AddTest(TestClassInfo testClass)
	{
		Classes.Add(testClass);
	}

	public static MethodInfo GetTestMethod(Type type, string name)
	{
		const BindingFlags flags = BindingFlags.Instance
			| BindingFlags.Static
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.FlattenHierarchy;

		var methods = type.GetMethods(flags);
		MethodInfo declared = null;
		MethodInfo inherited = null;

		for (var i = 0; i < methods.Length; i++)
		{
			var method = methods[i];
			if (method.Name != name)
			{
				continue;
			}

			if (method.DeclaringType == type)
			{
				if ((declared == null) || (method.GetParameters().Length == 0))
				{
					declared = method;
					if (method.GetParameters().Length == 0)
					{
						return method;
					}
				}
			}
			else if ((inherited == null) || (method.GetParameters().Length == 0))
			{
				inherited = method;
			}
		}

		return declared ?? inherited;
	}

	public void Process()
	{
		#if NET8_0_OR_GREATER
		var isAot = !RuntimeFeature.IsDynamicCodeSupported;
		#else
		var isAot = false; // or leave dynamic code path always on
		#endif

		var isReadyToRun = DetectReadyToRun();

		Console.OutputEncoding = new UTF8Encoding(false);
		Console.InputEncoding = new UTF8Encoding(false);

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
			[37m          Args: [0m{string.Join(", ", _args)}
			[37m       Bitness: [0m{(Environment.Is64BitProcess ? "x64" : "x86")}
			[37m       Machine: [0m{Environment.MachineName}
			""");

		if (GetPhysicallyInstalledSystemMemory(out var memory))
		{
			Console.WriteLine($"\e[37m        Memory: \e[0m{memory / 1024 / 1024} GB");
		}

		Console.WriteLine(
			$"""
			[37m  AOT (native): [0m{isAot}
			[37m    ReadyToRun: [0m{isReadyToRun}
			[37m        Filter: [0m{Filter}
			[37m         Quiet: [0m{Quiet}
			[37m       Verbose: [0m{Verbose}
			""");

		Console.WriteLine();

		// Execute AssemblyInitialize from all classes that have it
		foreach (var testClass in Classes)
		{
			if (testClass.AssemblyInitializeMethod?.MethodInfo != null)
			{
				try
				{
					testClass.AssemblyInitializeMethod.MethodInfo.Invoke(null, [null]);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"\e[97;41m AssemblyInitialize Failed: {ex.InnerException?.Message ?? ex.Message} \e[0m");
					return;
				}
			}
		}

		var totalPassed = 0;
		var totalFailed = 0;
		var totalSkipped = 0;
		var originalOut = Console.Out;
		var totalWatch = Stopwatch.StartNew();

		foreach (var testClass in Classes)
		{
			if (isAot && testClass.SkipInAot)
			{
				if (!Quiet)
				{
					Console.WriteLine($"{"",12} \e[93mSkipped (AOT)\e[0m: {testClass.ClassName}  – {testClass.SkipInAotReason}");
				}

				totalSkipped += testClass.TestMethods.Length;
				continue;
			}

			if (Filter is { Length: >= 1 }
				&& (testClass.ClassName != Filter[0]))
			{
				continue;
			}

			var builder = new StringBuilder(16384);
			using var stringWriter = new StringWriter(builder);

			try
			{
				InvokeOptionalStatic(testClass.ClassInitializeMethod);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\e[97;41m ClassInitialize Failed: {testClass.ClassName}: {ex.InnerException?.Message ?? ex.Message} \e[0m");
				continue;
			}

			foreach (var method in testClass.TestMethods)
			{
				if (Filter is { Length: >= 2 }
					&& (method.Name != Filter[1]))
				{
					continue;
				}

				if (isAot && method.SkipInAot)
				{
					if (!Quiet)
					{
						Console.WriteLine($"{"",7} \e[93mAOT Skipped\e[0m: {testClass.ClassName}.{method.Name} – {method.SkipInAotReason}");
					}

					totalSkipped++;
					continue;
				}

				var methodInfo = method.MethodInfo;
				if (methodInfo == null)
				{
					Console.WriteLine($"Inconclusive: {testClass.ClassName}.{method.Name} could not get method info...");
					continue;
				}

				if (TryGetIgnoreMessage(methodInfo, out var ignoreMessage))
				{
					if (!Quiet)
					{
						Console.WriteLine($"{"",12} \e[93mSkipped\e[0m: {testClass.ClassName}.{method.Name}{(string.IsNullOrEmpty(ignoreMessage) ? "" : $" – {ignoreMessage}")}");
					}

					totalSkipped++;
					continue;
				}

				var invocations = GetInvocationArguments(methodInfo);
				for (var rowIndex = 0; rowIndex < invocations.Count; rowIndex++)
				{
					builder.Clear();

					var testWatch = Stopwatch.StartNew();
					Console.SetOut(stringWriter);
					var displayName = invocations.Count == 1
						? $"{testClass.ClassName}.{method.Name}"
						: $"{testClass.ClassName}.{method.Name}[{rowIndex}]";

					try
					{
						async Task RunInvocation()
						{
							var instance = testClass.ConstructorInfo.Invoke([]);
							if (instance == null)
							{
								throw new InvalidOperationException($"{testClass.ClassName} could not create class instance.");
							}

							try
							{
								testClass.InitializeMethod?.MethodInfo.Invoke(instance, []);
								await InvokeTestMethod(methodInfo, instance, invocations[rowIndex]);
								testClass.CleanupMethod?.MethodInfo.Invoke(instance, []);
							}
							finally
							{
								(instance as IDisposable)?.Dispose();
							}
						}

						if (ShouldDispatchToHeadless(methodInfo))
						{
							DispatchToHeadless(methodInfo.DeclaringType?.Assembly ?? methodInfo.Module.Assembly, RunInvocation);
						}
						else
						{
							RunInvocation().GetAwaiter().GetResult();
						}

						testWatch.Stop();
						totalPassed++;
						Console.SetOut(originalOut);
						if (!Quiet)
						{
							Console.WriteLine($"{$"{testWatch.Elapsed.TotalMilliseconds:F4} ms",12} \e[92mPassed\e[0m: {displayName}");
							if (Verbose && (builder.Length > 0))
							{
								Console.WriteLine($"\e[96m{builder}\e[0m");
							}
						}
					}
					catch (Exception ex)
					{
						totalFailed++;
						Console.SetOut(originalOut);
						Console.WriteLine($"{$"{testWatch.Elapsed.TotalMilliseconds:F4} ms",12} \e[91mFailed\e[0m: {displayName}");
						Console.WriteLine($"\t\e[97;41m{ex}\e[0m");
						if (builder.Length > 0)
						{
							Console.WriteLine($"\e[96m{builder}\e[0m");
						}
					}
				}
			}

			try
			{
				InvokeOptionalStatic(testClass.ClassCleanupMethod);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"\e[97;41m ClassCleanup Failed: {testClass.ClassName}: {ex.InnerException?.Message ?? ex.Message} \e[0m");
			}
		}

		Console.WriteLine();
		Console.Write($"{$"{totalWatch.Elapsed.TotalMilliseconds:F4} ms",12} Elapsed, {totalPassed} Passed,");
		Console.Write(totalFailed == 0 ? $" {totalFailed} Failed" : $"\e[97;41m {totalFailed} Failed \e[0m");
		Console.WriteLine(totalSkipped == 0 ? "" : $", {totalSkipped} Skipped");
		Console.WriteLine();
	}

	private static List<object[]> GetInvocationArguments(MethodInfo method)
	{
		var rows = new List<object[]>();

		foreach (var attribute in method.GetCustomAttributes(true))
		{
			if (attribute.GetType().Name == "DataRowAttribute")
			{
				var data = attribute.GetType().GetProperty("Data")?.GetValue(attribute) as object[];
				rows.Add(data ?? []);
				continue;
			}

			if (IsTestMethodAttribute(attribute.GetType()))
			{
				continue;
			}

			var getData = attribute.GetType().GetMethod("GetData", [typeof(MethodInfo)]);
			if (getData == null)
			{
				continue;
			}

			if (getData.Invoke(attribute, [method]) is not System.Collections.IEnumerable enumerable)
			{
				continue;
			}

			foreach (var item in enumerable)
			{
				if (item is object[] row)
				{
					rows.Add(row);
				}
			}
		}

		if (rows.Count == 0)
		{
			rows.Add([]);
		}

		return rows;
	}

	private static void InvokeOptionalStatic(TestMethodInfo method)
	{
		if (method?.MethodInfo == null)
		{
			return;
		}

		var length = method.MethodInfo.GetParameters().Length;
		method.MethodInfo.Invoke(null, length == 0 ? [] : new object[length]);
	}

	private static object[] CoerceArguments(MethodInfo method, object[] arguments)
	{
		var parameters = method.GetParameters();
		arguments ??= [];
		if (parameters.Length == 0)
		{
			return [];
		}

		var last = parameters[parameters.Length - 1];
		var isParams = last.GetCustomAttributes(typeof(ParamArrayAttribute), true).Length > 0;
		var fixedCount = isParams ? parameters.Length - 1 : parameters.Length;

		if (isParams)
		{
			if ((arguments.Length == parameters.Length) && last.ParameterType.IsInstanceOfType(arguments[fixedCount]))
			{
				return FillOptional(parameters, arguments, parameters.Length);
			}

			var packed = new object[parameters.Length];
			for (var i = 0; i < fixedCount; i++)
			{
				packed[i] = i < arguments.Length ? arguments[i] : GetParameterDefault(parameters[i]);
			}

			var extra = Math.Max(0, arguments.Length - fixedCount);
			var elementType = last.ParameterType.GetElementType() ?? typeof(object);
			var array = Array.CreateInstance(elementType, extra);
			for (var i = 0; i < extra; i++)
			{
				array.SetValue(arguments[fixedCount + i], i);
			}

			packed[fixedCount] = array;
			return packed;
		}

		if (arguments.Length == parameters.Length)
		{
			return arguments;
		}

		return FillOptional(parameters, arguments, parameters.Length);
	}

	private static object[] FillOptional(ParameterInfo[] parameters, object[] arguments, int length)
	{
		var result = new object[length];
		for (var i = 0; i < length; i++)
		{
			result[i] = i < arguments.Length ? arguments[i] : GetParameterDefault(parameters[i]);
		}

		return result;
	}

	private static object GetParameterDefault(ParameterInfo parameter)
	{
		if (parameter.HasDefaultValue)
		{
			return parameter.DefaultValue;
		}

		return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
	}

	private static void DispatchToHeadless(Assembly assembly, Func<Task> action)
	{
		Type sessionType = null;
		foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
		{
			sessionType = loaded.GetType("Cornerstone.Presentation.Headless.HeadlessUnitTestSession", false);
			if (sessionType != null)
			{
				break;
			}
		}

		if (sessionType == null)
		{
			throw new InvalidOperationException("HeadlessUnitTestSession was not found. Headless tests cannot run.");
		}

		var getOrStart = sessionType.GetMethod("GetOrStartForAssembly", [typeof(Assembly)]);
		if (getOrStart == null)
		{
			throw new InvalidOperationException("HeadlessUnitTestSession.GetOrStartForAssembly was not found.");
		}

		var session = getOrStart.Invoke(null, [assembly]);
		var dispatch = FindHeadlessDispatchAsync(sessionType);
		if ((session == null) || (dispatch == null))
		{
			throw new InvalidOperationException("HeadlessUnitTestSession.Dispatch was not found.");
		}

		Func<Task<int>> wrapped = async () =>
		{
			await action();
			return 0;
		};

		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
		if (dispatch.Invoke(session, [wrapped, timeout.Token]) is Task task)
		{
			task.GetAwaiter().GetResult();
		}
	}

	private static MethodInfo FindHeadlessDispatchAsync(Type sessionType)
	{
		foreach (var method in sessionType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
		{
			if ((method.Name != "Dispatch") || !method.IsGenericMethodDefinition)
			{
				continue;
			}

			var parameters = method.GetParameters();
			if (parameters.Length != 2)
			{
				continue;
			}

			var first = parameters[0].ParameterType;
			if (!first.IsGenericType || (first.GetGenericTypeDefinition() != typeof(Func<>)))
			{
				continue;
			}

			var funcResult = first.GetGenericArguments()[0];
			if (!funcResult.IsGenericType || (funcResult.GetGenericTypeDefinition() != typeof(Task<>)))
			{
				continue;
			}

			if (parameters[1].ParameterType != typeof(CancellationToken))
			{
				continue;
			}

			return method.MakeGenericMethod(typeof(int));
		}

		return null;
	}

	private static bool ShouldDispatchToHeadless(MethodInfo method)
	{
		foreach (var attribute in method.GetCustomAttributes(true))
		{
			var property = attribute.GetType().GetProperty(
				"DispatchToHeadlessSession",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			if ((property != null) && (property.PropertyType == typeof(bool)) && Equals(property.GetValue(attribute), true))
			{
				return true;
			}

			if (attribute.GetType().Name == "HeadlessTestMethodAttribute")
			{
				return true;
			}
		}

		return false;
	}

	private static Task InvokeTestMethod(MethodInfo method, object instance, object[] arguments)
	{
		var result = method.Invoke(instance, CoerceArguments(method, arguments));
		if (result is Task task)
		{
			return task;
		}

		return Task.CompletedTask;
	}

	private static bool IsTestMethodAttribute(Type type)
	{
		while (type != null)
		{
			if (type.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
			{
				return true;
			}

			type = type.BaseType;
		}

		return false;
	}

	private static bool TryGetIgnoreMessage(MethodInfo method, out string message)
	{
		foreach (var attribute in method.GetCustomAttributes(true))
		{
			var type = attribute.GetType();
			if (type.Name == "IgnoreAttribute")
			{
				message = type.GetProperty("IgnoreMessage")?.GetValue(attribute) as string
					?? type.GetProperty("Message")?.GetValue(attribute) as string;
				return true;
			}

			if (!IsTestMethodAttribute(type))
			{
				continue;
			}

			var skip = type.GetProperty("Skip")?.GetValue(attribute) as string;
			if (!string.IsNullOrEmpty(skip))
			{
				message = skip;
				return true;
			}
		}

		message = null;
		return false;
	}

	private static bool DetectReadyToRun()
	{
		try
		{
			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var location = assembly.Location;

			if (string.IsNullOrEmpty(location) || !File.Exists(location))
			{
				return false; // Single-file or Native AOT usually has empty Location
			}

			// Look for the ReadyToRun signature in the PE
			// R2R images contain the string "RTR" or the ReadyToRunHeader magic
			using var fs = File.OpenRead(location);
			using var reader = new BinaryReader(fs);

			// Simple & fast heuristic that works well in practice:
			// Search for the "ReadyToRun" magic or the common R2R section marker
			const int bufferSize = 4096;
			var buffer = new byte[bufferSize];
			int bytesRead;

			while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
			{
				// Look for the ASCII sequence "RTR\0" or "ReadyToRun"
				for (var i = 0; i < (bytesRead - 3); i++)
				{
					if ((buffer[i] == (byte) 'R') &&
						(buffer[i + 1] == (byte) 'T') &&
						(buffer[i + 2] == (byte) 'R'))
					{
						return true;
					}
				}
			}
		}
		catch
		{
			// Ignore – if we can't read the file we just assume non-R2R
		}

		return false;
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
				case "-q" or "--quiet" or "--failed" or "--failed-only":
				{
					Quiet = true;
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

	public TestMethodInfo AssemblyInitializeMethod { get; init; }
	public TestMethodInfo ClassCleanupMethod { get; init; }
	public TestMethodInfo ClassInitializeMethod { get; init; }
	public string ClassName { get; init; }
	public TestMethodInfo CleanupMethod { get; init; }
	public ConstructorInfo ConstructorInfo { get; init; }
	public TestMethodInfo InitializeMethod { get; init; }
	public bool SkipInAot { get; init; }
	public string SkipInAotReason { get; init; }
	public TestMethodInfo[] TestMethods { get; init; }

	#endregion
}

public class TestMethodInfo
{
	#region Properties

	public MethodInfo MethodInfo { get; init; }
	public string Name { get; init; }
	public bool SkipInAot { get; init; }
	public string SkipInAotReason { get; init; }

	#endregion
}