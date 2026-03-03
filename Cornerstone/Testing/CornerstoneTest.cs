#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text.CodeGenerators;

#endregion

#pragma warning disable IL2026
#pragma warning disable IL2070

namespace Cornerstone.Testing;

/// <summary>
/// The base test for the Cornerstone framework.
/// </summary>
[SourceReflection]
public abstract class CornerstoneTest : DependencyProvider, IDateTimeProvider
{
	#region Fields

	private DateTime? _currentDateTime;
	private readonly IDateTimeProvider _defaultDateTimeProvider;
	private IDateTimeProvider _overriddenDateTimeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the Cornerstone test.
	/// </summary>
	protected CornerstoneTest() : base("Unit Test")
	{
		_defaultDateTimeProvider = new DateTimeProvider(
			Guid.Parse("9C20125A-8E7A-428C-BA1C-9BDB72B7A543"),
			() => _overriddenDateTimeProvider?.UtcNow ?? _currentDateTime ?? DateTime.UtcNow
		);
		_currentDateTime = StartDateTime;
	}

	static CornerstoneTest()
	{
		// Default start date
		StartDateTime = new DateTime(2000, 01, 02, 03, 04, 00, DateTimeKind.Utc);
	}

	#endregion

	#region Properties

	public bool EnableFileUpdates { get; protected set; }

	/// <summary>
	/// Returns true if the debugger is attached.
	/// </summary>
	public static bool IsDebugging => Debugger.IsAttached;

	public DateTime Now => _defaultDateTimeProvider.Now;

	public string SourceFileName => GetTestSourceFileName();

	/// <summary>
	/// Represents the Cornerstone initial start time. Resets on test reset.
	/// </summary>
	public static DateTime StartDateTime { get; }

	public DateTime UtcNow => _defaultDateTimeProvider.UtcNow;

	/// <summary>
	/// The timeout to use when waiting for a test state to be hit.
	/// </summary>
	public static TimeSpan WaitTimeout => TimeSpan.FromMilliseconds(IsDebugging ? 1000000 : 1000);

	#endregion

	#region Methods

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public static void AreEqual<T>(T expected, T actual, Func<string> message = null, ComparerSettings? settings = null, Action<CompareSession<T, T>> configure = null)
	{
		var session = Compare(expected, actual, settings, configure);
		session.Assert(CompareResult.AreEqual, message);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public static void AreNotEqual<T>(T expected, T actual, Func<string> message = null, ComparerSettings? settings = null, Action<CompareSession<T, T>> configure = null)
	{
		using var session = Compare(expected, actual, settings, configure);
		session.Assert(CompareResult.NotEqual, message);
	}

	/// <summary>
	/// Test for an expected exception.
	/// </summary>
	/// <typeparam name="T"> The type of the exception. </typeparam>
	/// <param name="work"> The test. </param>
	/// <param name="messages"> A set of messages where at least one message should be found. </param>
	public static void ExpectedException<T>(Action work, params string[] messages) where T : Exception
	{
		ExpectedException<T>(work, null, messages);
	}

	/// <summary>
	/// Test for an expected exception.
	/// </summary>
	/// <typeparam name="T"> The type of the exception. </typeparam>
	/// <param name="work"> The test. </param>
	/// <param name="extraValidation"> An optional set of extra validation. </param>
	/// <param name="messages"> A set of messages where at least one message should be found. </param>
	public static void ExpectedException<T>(Action work, Action<T> extraValidation, params string[] messages) where T : Exception
	{
		Exception otherException = null;

		try
		{
			work();
		}
		catch (T ex)
		{
			var detailedException = ex.ToDetailedString();
			var allErrors = "\"" + string.Join("\", \"", messages) + "\"";

			if ((messages.Length > 0) && !messages.Any(detailedException.Contains))
			{
				throw new CornerstoneException($"Actual <{detailedException}> does not contain expected <{allErrors}>.");
			}

			extraValidation?.Invoke(ex);
			return;
		}
		catch (Exception ex)
		{
			otherException = ex;
		}

		throw new CornerstoneException(
			otherException != null
				? $"The expected exception was not thrown. {otherException.GetType()} thrown instead."
				: "The expected exception was not thrown."
		);
	}

	/// <summary>
	/// Fail the test with a <see cref="CornerstoneException" />.
	/// </summary>
	/// <param name="message"> The message to fail with. </param>
	[DoesNotReturn]
	public static void Fail(string message)
	{
		throw new CornerstoneException(message);
	}

	/// <summary>
	/// Increment the current time. This only works if current time is set. Negative values will subtract time.
	/// </summary>
	/// <param name="years"> The years to increment by. </param>
	/// <param name="months"> The months to increment by. </param>
	/// <param name="days"> The days to increment by. </param>
	/// <param name="hours"> The hours to increment by. </param>
	/// <param name="minutes"> The minutes to increment by. </param>
	/// <param name="seconds"> The seconds to increment by. </param>
	/// <param name="milliseconds"> The milliseconds to increment by. </param>
	/// <param name="microseconds"> The microseconds to increment by. Only supported in .NET 7 or greater. </param>
	/// <param name="ticks"> The ticks to increment by. </param>
	public DateTime IncrementTime(int years = 0, int months = 0, int days = 0, int hours = 0, int minutes = 0, int seconds = 0, int milliseconds = 0, int microseconds = 0, long ticks = 0)
	{
		var currentTime = _defaultDateTimeProvider.UtcNow;

		if (years != 0)
		{
			currentTime = currentTime.AddYears(years);
		}

		if (months != 0)
		{
			currentTime = currentTime.AddMonths(months);
		}

		if (days != 0)
		{
			currentTime += TimeSpan.FromDays(days);
		}

		if (hours != 0)
		{
			currentTime += TimeSpan.FromHours(hours);
		}

		if (minutes != 0)
		{
			currentTime += TimeSpan.FromMinutes(minutes);
		}

		if (seconds != 0)
		{
			currentTime += TimeSpan.FromSeconds(seconds);
		}

		if (milliseconds != 0)
		{
			currentTime += TimeSpan.FromMilliseconds(milliseconds);
		}

		if (microseconds != 0)
		{
			currentTime += TimeSpan.FromMicroseconds(microseconds);
		}

		if (ticks != 0)
		{
			currentTime += TimeSpan.FromTicks(ticks);
		}

		return SetTime(currentTime);
	}

	/// <summary>
	/// Increment the current time. This only works if current time is set. Negative values will subtract time.
	/// </summary>
	/// <param name="value"> The value to increment by. </param>
	public DateTime IncrementTime(TimeSpan value)
	{
		var currentTime = UtcNow;
		return SetTime(currentTime + value);
	}

	/// <summary>
	/// Tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be false. </param>
	/// <param name="message"> The message is shown in test results. </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void IsFalse([DoesNotReturnIf(true)] bool condition, Func<string> message = null)
	{
		if (condition)
		{
			throw new CornerstoneException(message?.Invoke() ?? "The condition was incorrectly true and should have been false.");
		}
	}

	/// <summary>
	/// Tests whether the specified condition is not null and throws an exception if the condition is null.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be not null. </param>
	/// <param name="message"> The message is shown in test results. </param>
	public void IsNotNull([NotNull] object condition, Func<string> message = null)
	{
		if (condition == null)
		{
			throw new CornerstoneException(message?.Invoke() ?? "The condition was incorrectly null and should have been not null.");
		}
	}

	/// <summary>
	/// Tests whether the specified condition is null and throws an exception if the condition is not null.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be null. </param>
	/// <param name="message"> The message is shown in test results. </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void IsNull(object condition, Func<string> message = null)
	{
		if (condition != null)
		{
			throw new CornerstoneException(message?.Invoke() ?? "The condition was incorrectly not null and should have been null.");
		}
	}

	/// <summary>
	/// Tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be true. </param>
	/// <param name="message"> The message is shown in test results. </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void IsTrue([DoesNotReturnIf(false)] bool condition, Func<string> message = null)
	{
		if (!condition)
		{
			throw new CornerstoneException(message?.Invoke() ?? "The condition was incorrectly false and should have been true.");
		}
	}

	/// <summary>
	/// Reset the current time to the provided time or back to real time.
	/// </summary>
	/// <param name="currentTime"> An optional current time to reset to otherwise back to DateTime.UtcNow and Now. </param>
	public void ResetCurrentTime(DateTime? currentTime = null)
	{
		_currentDateTime = currentTime;
	}

	/// <summary>
	/// Set the CurrentTime value.
	/// </summary>
	/// <param name="provider"> The provider for time. </param>
	public DateTime SetTime(Func<DateTime> provider)
	{
		return SetTime(new DateTimeProvider(provider));
	}

	/// <summary>
	/// Set the CurrentTime value.
	/// </summary>
	/// <param name="provider"> The provider for time. </param>
	public DateTime SetTime(DateTimeProvider provider)
	{
		_overriddenDateTimeProvider = provider;
		return UtcNow;
	}

	/// <summary>
	/// Set the time to a specific value.
	/// </summary>
	/// <param name="value"> The new time to set. </param>
	public DateTime SetTime(DateTime value)
	{
		_overriddenDateTimeProvider = null;
		_currentDateTime = value.ToUtcDateTime();
		return UtcNow;
	}

	/// <summary>
	/// Cleanup the test.
	/// </summary>
	public virtual void TestCleanup()
	{
	}

	/// <summary>
	/// Initialize the test.
	/// </summary>
	public virtual void TestInitialize()
	{
		ResetCurrentTime(StartDateTime);
		Babel.Tower.Reset();
	}

	/// <summary>
	/// Compare two objects.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <typeparam name="T2"> The data type of the actual value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	protected static CompareSession<T, T2> Compare<T, T2>(T expected, T2 actual, ComparerSettings? settings = null, Action<CompareSession<T, T2>> configure = null)
	{
		var session = Comparer.StartSession(expected, actual, settings);
		configure?.Invoke(session);
		session.Compare();
		return session;
	}

	protected void ExpectedCode<T>(T expected, T actual, [CallerMemberName] string callingMethodName = "")
	{
		using var session = Compare(expected, actual);
		if (session.Result != CompareResult.AreEqual)
		{
			if (!string.IsNullOrWhiteSpace(SourceFileName)
				&& (EnableFileUpdates || IsDebugging))
			{
				var sourceFileInfo = new FileInfo(SourceFileName);
				UpdateFile(callingMethodName, sourceFileInfo,
					builder =>
					{
						builder.IndentWrite("var expected = ");
						builder.WriteObject(actual);
						builder.IndentWrite(string.Empty);
					});
				Fail("Test updated...");
				return;
			}
		}
		session.Assert(CompareResult.AreEqual);
	}

	/// <summary>
	/// Validate all values with the provided validator.
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="values"> </param>
	/// <param name="validate"> </param>
	protected void ForEach<T>(T[] values, Action<T> validate)
	{
		foreach (var item in values)
		{
			validate(item);
		}
	}

	protected string GetTestSourceFileName()
	{
		// Create a stack trace with file information
		var stackTrace = new StackTrace(true);

		// Iterate through frames to find the first method not in TestBase
		foreach (var frame in stackTrace.GetFrames())
		{
			var method = frame.GetMethod();
			var attributes = method?.GetCustomAttributes(false).Cast<Attribute>();

			// Check if the method is in a derived class (not TestBase or its base class)
			if (attributes.Any(x =>
					x.GetType().Name
						is "TestMethodAttribute"
						or "TestAttribute"
						or "TestCaseAttribute"))
			{
				return frame.GetFileName();
			}
		}

		// Fallback if no suitable frame is found
		return null;
	}

	protected void UpdateableShouldUpdateAll(ComparerSettings settings, Type sourceType, Type destinationType, IncludeExcludeSettings includeExcludeSettings)
	{
		if (sourceType != destinationType)
		{
			$"\t> {sourceType.FullName}".Dump();
		}

		var destinationSourceType = SourceReflector.GetRequiredSourceType(destinationType);
		var destinationWithDefaults = GetModelWithDefaultValues(destinationSourceType, includeExcludeSettings);
		if (destinationWithDefaults is IRequiresDateTimeProvider requiresWithDefaults)
		{
			requiresWithDefaults.UpdateDateTimeProvider(this);
		}
		ValidateUpdateWith(destinationSourceType, destinationSourceType, destinationWithDefaults, settings);

		var destinationWithNonDefaults = GetModelWithNonDefaultValues(destinationSourceType, includeExcludeSettings);
		if (destinationWithNonDefaults is IRequiresDateTimeProvider requiresNonDefault)
		{
			requiresNonDefault.UpdateDateTimeProvider(this);
		}
		ValidateUpdateWith(destinationSourceType, destinationSourceType, destinationWithNonDefaults, settings);

		if (sourceType == destinationType)
		{
			return;
		}

		var sourceSourceType = SourceReflector.GetRequiredSourceType(sourceType);
		var sourceWithDefaults = GetModelWithDefaultValues(sourceSourceType, includeExcludeSettings);
		if (sourceWithDefaults is IRequiresDateTimeProvider requiresWithDefaults2)
		{
			requiresWithDefaults2.UpdateDateTimeProvider(this);
		}

		var sourceWithNonDefaults = GetModelWithNonDefaultValues(sourceSourceType, includeExcludeSettings);
		if (sourceWithNonDefaults is IRequiresDateTimeProvider requiresNonDefault2)
		{
			requiresNonDefault2.UpdateDateTimeProvider(this);
		}

		ValidateUpdateWith(destinationSourceType, sourceSourceType, sourceWithDefaults, settings);
		ValidateUpdateWith(destinationSourceType, sourceSourceType, sourceWithNonDefaults, settings);
	}

	protected void UpdateableShouldUpdateAll(bool updateCodeGeneratedFiles, Type updateableType, Assembly[] assemblies,
		List<Type> exclusions, ComparerSettings settings, Func<Type, bool> additionalTypeFilter = null)
	{
		var assemblyTypes = assemblies.SelectMany(s => s.GetTypes()).Where(t => (t != null) && updateableType.IsAssignableFrom(t)).ToList();

		var autoExclusions = assemblyTypes.Where(x => !x.IsInterface && x.GetProperties().All(p => !p.GetCustomAttributes<UpdateableActionAttribute>().Any())).ToList();

		exclusions.AddRange(autoExclusions);

		var types = assemblyTypes.Where(t => !exclusions.Contains(t)).Where(additionalTypeFilter ?? (_ => true)).Where(x =>
		{
			if (x.IsAbstract || x.IsInterface || x.ContainsGenericParameters)
			{
				return false;
			}

			if (x.GetConstructors().All(t => t.GetParameters().Any()))
			{
				// Ignore if the type does not have an empty constructor
				return false;
			}

			var s = SourceReflector.GetRequiredSourceType(x);
			return s.GetMethods().Any(m => m.Name == nameof(IUpdateable.UpdateWith));
		}).ToArray();

		var updateableSourceTypes = types.ToDictionary(x => x,
			x => x
				.GetInterfaces().Where(m =>
					m.IsDirectDescendantOf(typeof(IUpdateable))
					&& (m.GenericTypeArguments.Length > 0)
					&& !m.IsInterface
				).Select(t => t.GenericTypeArguments[0]).Distinct().ToList()
		).Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

		foreach (var destinationType in types)
		{
			UpdateableShouldUpdateAll(settings, destinationType, updateableSourceTypes);
		}
	}

	protected void UpdateableShouldUpdateAll(ComparerSettings settings, Type destinationType, Dictionary<Type, List<Type>> updateableSourceTypes)
	{
		destinationType.FullName.Dump();

		var includeExcludeSettings = settings
			.TypeIncludeExcludeSettings
			.TryGetValue(destinationType, out var foundExclusions)
			? foundExclusions
			: IncludeExcludeSettings.Empty;

		foreach (var typeKey in settings.TypeIncludeExcludeSettings.Keys)
		{
			if (!destinationType.ImplementsType(typeKey))
			{
				continue;
			}

			includeExcludeSettings = includeExcludeSettings
				.WithMoreSettings(settings.TypeIncludeExcludeSettings[typeKey]);
		}

		var typesToTest = new HashSet<Type>([destinationType]);
		if (updateableSourceTypes.TryGetValue(destinationType, out var additional))
		{
			foreach (var type in additional)
			{
				typesToTest.Add(type);
			}
		}

		foreach (var sourceType in typesToTest)
		{
			UpdateableShouldUpdateAll(settings, sourceType, destinationType, includeExcludeSettings);
		}
	}

	protected void UpdateFile(string methodName, FileInfo fileInfo, Action<CodeBuilder> build)
	{
		UpdateFile(fileInfo, methodName, "// Generated Code - {0}", "Expected", build);
	}

	protected void UpdateFile(FileInfo fileInfo, string method, string sectionFormat, string sectionName, Action<CodeBuilder> build)
	{
		if (fileInfo is not { Exists: true })
		{
			Fail("File not found?");
		}

		var content = File.ReadAllText(fileInfo.FullName);
		var methodTemplate = $" {method}(";
		var methodIndex = content.IndexOf(methodTemplate);
		if (methodIndex < 0)
		{
			Fail("File method not found.");
		}

		var startTemplate = string.Format(sectionFormat, sectionName);
		var startIndex = content.IndexOf(startTemplate, methodIndex);
		if (startIndex < 0)
		{
			Fail("File section start not found.");
		}

		var endTemplate = string.Format(sectionFormat, $"/{sectionName}");
		var endIndex = content.IndexOf(endTemplate, startIndex);
		if ((endIndex <= 0) || (endIndex <= startIndex))
		{
			Fail("File section end not found.");
		}

		var currentSection = content.Substring(startIndex + startTemplate.Length, endIndex - startIndex - startTemplate.Length).TrimStart('\r', '\n');
		var builder = new CodeBuilder();
		builder.TryDetectIndent(currentSection);
		build(builder);
		builder.IndentWrite(string.Empty);

		var newSection = builder.ToString();
		content = content
			.Remove(startIndex + startTemplate.Length, endIndex - startIndex - startTemplate.Length)
			.Insert(startIndex + startTemplate.Length, newSection);

		File.WriteAllText(fileInfo.FullName, content, Encoding.UTF8);
	}

	protected void ValidateExpected(string expected, string actual,
		[CallerMemberName] string callingMethodName = "")
	{
		if ((EnableFileUpdates || IsDebugging)
			&& !string.Equals(expected, actual))
		{
			var sourceFileInfo = new FileInfo(SourceFileName);
			UpdateFile(callingMethodName, sourceFileInfo,
				builder =>
				{
					builder.WriteLine();
					builder.IndentWriteLine("var expected =");
					builder.IncreaseIndent();
					builder.IndentWriteLine("\"\"\"");

					var lines = actual.Split("\r\n");
					foreach (var line in lines)
					{
						builder.IndentWriteLine(line);
					}

					builder.IndentWriteLine("\"\"\";");
					builder.DecreaseIndent();
				});
		}

		AreEqual(expected, actual);
	}

	protected void ValidatePerformance(
		string name,
		Action test,
		ulong maxTime,
		long maxBytes,
		int warmUpIterations = 40,
		int measuredIterations = 200,
		Action afterWarmup = null)
	{
		//Debug.Assert(!Assembly.GetExecutingAssembly().IsAssemblyDebugBuild());
		//Debug.Assert(!Assembly.GetCallingAssembly().IsAssemblyDebugBuild());

		// Warmup + tiered compilation stabilization
		for (var i = 0; i < warmUpIterations; i++)
		{
			test();
		}

		afterWarmup?.Invoke();

		// Heavy GC stabilization
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

		var allocStart = GC.GetTotalAllocatedBytes(true);
		var times = new List<long>(measuredIterations);

		for (var i = 0; i < measuredIterations; i++)
		{
			var t0 = Stopwatch.GetTimestamp();
			test();
			var t1 = Stopwatch.GetTimestamp();
			times.Add(t1 - t0);
		}

		var allocEnd = GC.GetTotalAllocatedBytes(true);
		var sortedNs = times.Select(t => (t * 1_000_000_000.0) / Stopwatch.Frequency).OrderBy(x => x).ToList();

		var avgNs = sortedNs.Average();
		var averageTs = TimeSpanExtensions.ToTimeSpan(avgNs);
		var p95Ns = sortedNs[(int) (sortedNs.Count * 0.95)];
		var p95Ts = TimeSpanExtensions.ToTimeSpan(p95Ns);
		var allocByte = (allocEnd - allocStart) / measuredIterations;

		Console.WriteLine(name);
		Console.WriteLine("\tTime");
		Console.WriteLine($"\t\tavg : {averageTs}");
		Console.WriteLine($"\t\tp95 : {p95Ts}");
		Console.WriteLine("\tAllocated");
		Console.WriteLine($"\t\tavg : {allocByte:N0} B");
		Console.WriteLine();

		if (avgNs > maxTime)
		{
			throw new Exception($"Timing failed: {averageTs} > {maxTime:N0} ns (p95: {p95Ts})");
		}

		if (allocByte > maxBytes)
		{
			throw new Exception($"Allocation failed: {allocByte:N0} B > {maxBytes:N0} B");
		}
	}

	/// <summary>
	/// Validate a model's UpdateWith using the "IUpdateable" interface.
	/// </summary>
	/// <param name="destinationType"> The type of the destination. </param>
	/// <param name="sourceType"> The type of the source. </param>
	/// <param name="source"> The source update to apply to the destination. </param>
	/// <param name="comparerSettings"> The setting for comparing. </param>
	protected void ValidateUpdateWith(SourceTypeInfo destinationType, SourceTypeInfo sourceType, object source, ComparerSettings comparerSettings)
	{
		var actions = UpdateableExtensions.GetUpdateableActions();

		foreach (var action in actions)
		{
			var destinationInstance = SourceReflector.CreateInstance(destinationType);

			IsNotNull(destinationInstance);

			if (destinationInstance is IRequiresDateTimeProvider requires)
			{
				requires.UpdateDateTimeProvider(this);
			}

			var destination = (IUpdateable) destinationInstance;
			var settings = Cache.GetSettings(destination, action);
			var result = destination?.UpdateWith(source, settings) ?? false;
			IsTrue(result, () => $"Failed to update {sourceType.Type.FullName} > {destinationType.Type.FullName} the destination with the source.");

			var areEqualSettings = comparerSettings; //.ShallowClone();

			if ((settings.ExcludedProperties.Count > 0)
				&& (settings.IncludedProperties.Count <= 0))
			{
				// todo: what could we check?
				// Everything is excluded and nothing is included, aka nothing to check
				return;
			}

			// todo: fix
			//areEqualSettings.TypeIncludeExcludeSettings.AddOrUpdate(destinationType.Type, () => settings, x => x.WithMoreSettings(settings));

			AreEqual(destination, source,
				() =>
				{
					$"\t > {action}".Dump();

					//CopyToClipboard(GenerateUpdateWith(destinationType, sourceType)).Dump();
					return destinationType.Type.FullName;
				},
				areEqualSettings
			);
		}
	}

	private object GetModelWithDefaultValues(SourceTypeInfo sourceTypeInfo, IncludeExcludeSettings settings)
	{
		var response = SourceReflector.CreateInstance(sourceTypeInfo);
		return response;
	}

	private object GetModelWithNonDefaultValues(SourceTypeInfo sourceTypeInfo, IncludeExcludeSettings settings)
	{
		var response = SourceReflector.CreateInstance(sourceTypeInfo);
		return response;
	}

	#endregion
}