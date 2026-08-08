#region References

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Cornerstone.Extensions;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Platforms;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Host-agnostic process bootstrap for Cornerstone (console, service, website, Avalonia, etc.).
/// Call <see cref="Initialize" /> once from the host entry point before resolving services.
/// </summary>
public static class AppBootstrap
{
	#region Constants

	/// <summary>
	/// Host argument key that enables one-shot startup profiling (<c>-ProfileStartup</c>).
	/// </summary>
	public const string ProfileStartupArgument = "ProfileStartup";

	#endregion

	#region Properties

	public static ApplicationArguments ApplicationArguments { get; private set; }

	public static IDateTimeProvider DateTimeProvider { get; private set; }

	public static DependencyProvider DependencyProvider { get; private set; }

	public static bool IsInitialized { get; private set; }

	public static RuntimeInformation RuntimeInformation { get; private set; }

	/// <summary>
	/// Optional one-shot startup session. Null when disabled (default). Set automatically when
	/// <see cref="ProfileStartupArgument" /> is present, or assign before <see cref="Initialize" />.
	/// </summary>
	public static StartupProfiler StartupProfiler { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Initialize if needed (design-time / tests that never hit host Main).
	/// </summary>
	public static void EnsureInitialized(
		string applicationName = "Design",
		Assembly applicationAssembly = null,
		string[] args = null,
		IDispatcher dispatcher = null)
	{
		if (IsInitialized)
		{
			return;
		}

		Initialize(
			applicationName,
			applicationAssembly ?? Assembly.GetCallingAssembly(),
			args,
			dispatcher,
			"Design"
		);
	}

	/// <summary>
	/// Resolve a registered service. Requires <see cref="Initialize" /> (or design-time ensure).
	/// </summary>
	public static T GetInstance<T>()
	{
		EnsureReady();
		return DependencyProvider.GetInstance<T>();
	}

	/// <summary>
	/// Resolve a registered service. Requires <see cref="Initialize" /> (or design-time ensure).
	/// </summary>
	public static object GetInstance(Type type)
	{
		EnsureReady();
		return DependencyProvider.GetInstance(type);
	}

	/// <summary>
	/// Create arguments, dependency provider, runtime information; register core services; initialize platform.
	/// Safe to call only once per process (subsequent calls throw).
	/// </summary>
	public static void Initialize(
		string applicationName,
		Assembly applicationAssembly,
		string[] args = null,
		IDispatcher dispatcher = null,
		string providerName = null)
	{
		if (IsInitialized)
		{
			throw new CornerstoneException($"{nameof(AppBootstrap)} is already initialized.");
		}

		if (string.IsNullOrWhiteSpace(applicationName))
		{
			throw new ArgumentException("Application name is required.", nameof(applicationName));
		}

		if (applicationAssembly == null)
		{
			throw new ArgumentNullException(nameof(applicationAssembly));
		}

		ApplicationArguments = new ApplicationArguments();
		DateTimeProvider = Runtime.DateTimeProvider.RealTime;
		DependencyProvider = new DependencyProvider(providerName ?? "Cornerstone");
		RuntimeInformation = new RuntimeInformation();

		RuntimeInformation.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), applicationName);

		if (args is { Length: > 0 })
		{
			ApplicationArguments.Parse(args);
		}

		if (ApplicationArguments.Exists(ProfileStartupArgument) && (StartupProfiler is null))
		{
			StartupProfiler = new StartupProfiler(DateTimeProvider);
		}

		using (StartupProfiler.Start("AppBootstrap.Initialize"))
		{
			using (StartupProfiler.Start("RuntimeInformation"))
			{
				RuntimeInformation.Initialize(applicationAssembly);
			}

			using (StartupProfiler.Start("Dependencies"))
			{
				DependencyProvider.AddSingleton(ApplicationArguments);
				DependencyProvider.SetupCornerstoneDependencies(
					dispatcher: dispatcher,
					runtimeInformation: RuntimeInformation
				);
			}

			using (StartupProfiler.Start("Platform"))
			{
				Platform.Initialize(DependencyProvider, RuntimeInformation);
			}
		}

		IsInitialized = true;
	}

	/// <summary>
	/// Ensure RuntimeInformation and IPlatform have completed Initialize + Load (idempotent).
	/// </summary>
	public static void InitializeInfrastructure()
	{
		EnsureReady();

		using (StartupProfiler.Start("Infrastructure.Initialize"))
		{
			if (!RuntimeInformation.IsLifecycleInitialized())
			{
				RuntimeInformation.InitializeLifecycle();
			}

			if (!RuntimeInformation.IsLifecycleLoaded())
			{
				RuntimeInformation.LoadLifecycle();
			}

			if (TryGetPlatform(out var platform))
			{
				if (!platform.IsLifecycleInitialized())
				{
					platform.InitializeLifecycle();
				}

				if (!platform.IsLifecycleLoaded())
				{
					platform.LoadLifecycle();
				}
			}
		}
	}

	/// <summary>
	/// Write a crash log under application data. No-op friendly if bootstrap is incomplete.
	/// </summary>
	public static void LogException(Exception ex)
	{
		if (ex == null)
		{
			return;
		}

		try
		{
			var runtimeInformation = IsInitialized
				? RuntimeInformation
				: DependencyProvider?.GetInstance<IRuntimeInformation>();

			if (runtimeInformation == null)
			{
				return;
			}

			var builder = new StringBuilder();
			builder.Append("Crash: ");
			builder.AppendLine(ex.Message);
			builder.AppendLine(ex.StackTrace);
			builder.AppendLine("----------------------------");
			builder.AppendLine(runtimeInformation.ToString());
			builder.AppendLine("----------------------------");

			var directory = Path.Combine(runtimeInformation.ApplicationDataLocation, "CrashLogs");
			new DirectoryInfo(directory).SafeCreate();

			var file = Path.Combine(directory, $"Crash-{DateTime.Now.Ticks:D20}.log");
			File.WriteAllText(file, builder.ToString());
		}
		catch
		{
			// Best-effort logging only.
		}
	}

	public static void RegisterAsTests(CornerstoneTest cornerstoneTest)
	{
		ApplicationArguments = new ApplicationArguments();
		DependencyProvider = cornerstoneTest;
		RuntimeInformation = cornerstoneTest.GetInstance<RuntimeInformation>();
		IsInitialized = true;
	}

	/// <summary>
	/// Clear bootstrap state. Intended for unit tests only.
	/// </summary>
	public static void Reset()
	{
		ApplicationArguments = null;
		DateTimeProvider = null;
		DependencyProvider = null;
		RuntimeInformation = null;
		StartupProfiler = null;
		IsInitialized = false;
	}

	/// <summary>
	/// Stop / unload / uninitialize Platform then RuntimeInformation.
	/// </summary>
	public static void ShutdownInfrastructure()
	{
		if (!IsInitialized)
		{
			return;
		}

		if (TryGetPlatform(out var platform))
		{
			TeardownLifecycle(platform);
		}

		TeardownLifecycle(RuntimeInformation);
	}

	/// <summary>
	/// Start RuntimeInformation and IPlatform (idempotent per lifecycle rules of each).
	/// </summary>
	public static void StartInfrastructure()
	{
		EnsureReady();

		using (StartupProfiler.Start("Infrastructure.Start"))
		{
			RuntimeInformation.StartLifecycle();

			if (TryGetPlatform(out var platform))
			{
				platform.StartLifecycle();
			}
		}
	}

	public static void TeardownLifecycle(ILifecycle lifecycle)
	{
		if (lifecycle == null)
		{
			return;
		}

		if (lifecycle.IsLifecycleStarted())
		{
			lifecycle.StopLifecycle();
		}

		if (lifecycle.IsLifecycleLoaded())
		{
			lifecycle.UnloadLifecycle();
		}

		if (lifecycle.IsLifecycleInitialized())
		{
			lifecycle.UninitializeLifecycle();
		}
	}

	/// <summary>
	/// Resolve the registered platform host, if present.
	/// </summary>
	public static bool TryGetPlatform(out IPlatform platform)
	{
		if (!IsInitialized || (DependencyProvider == null))
		{
			platform = null;
			return false;
		}

		return DependencyProvider.TryGetInstance(out platform);
	}

	private static void EnsureReady()
	{
		if (!IsInitialized || (DependencyProvider == null))
		{
			throw new CornerstoneException(
				$"{nameof(AppBootstrap)} has not been initialized. Call {nameof(Initialize)} from the host entry point."
			);
		}
	}

	#endregion
}