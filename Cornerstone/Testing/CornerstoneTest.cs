#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cornerstone.Attributes;
using Cornerstone.Compare;
using Cornerstone.Convert;
using Cornerstone.Convert.Converters;
using Cornerstone.Data;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Internal;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Settings;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Text;
#if WINDOWS
using Cornerstone.Platforms.Windows;
#endif

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// The base test for the Cornerstone framework.
/// </summary>
public abstract partial class CornerstoneTest : DependencyProvider, IDateTimeProvider
{
	#region Fields

	private static IClipboardService _clipboard;
	private DateTime? _currentDateTime;
	private readonly IDateTimeProvider _defaultDateTimeProvider;
	private IDateTimeProvider _overriddenDateTimeProvider;
	private UpdateableAction[] _updateableActions;

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

		RuntimeInformation = RuntimeInformationData.GetSample();
		EnableFileUpdates = false;
		RunTestAgainstDatabase = false;
	}

	static CornerstoneTest()
	{
		// Default start date
		StartDateTime = new DateTime(2000, 01, 02, 03, 04, 00, DateTimeKind.Utc);
		ClearDatabaseQuery = """
							EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
							EXEC sp_MSForEachTable 'ALTER TABLE ? DISABLE TRIGGER ALL'
							EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; IF ''?'' NOT LIKE ''%MigrationHistory%'' AND ''?'' NOT LIKE ''%MigrationsHistory%'' DELETE FROM ?'
							EXEC sp_MSforeachtable 'ALTER TABLE ? ENABLE TRIGGER ALL'
							EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'
							EXEC sp_MSForEachTable 'IF OBJECTPROPERTY(object_id(''?''), ''TableHasIdentity'') = 1 DBCC CHECKIDENT (''?'', RESEED, 0)'
							""";

		TempDirectory = Path.Combine(Path.GetTempPath(), "Cornerstone.UnitTests");
	}

	#endregion

	#region Properties

	public static string ClearDatabaseQuery { get; protected set; }

	public bool EnableFileUpdates { get; protected set; }

	/// <summary>
	/// Returns true if the debugger is attached.
	/// </summary>
	public static bool IsDebugging => Debugger.IsAttached;

	/// <inheritdoc />
	public DateTime Now => _defaultDateTimeProvider.Now;

	public bool RunTestAgainstDatabase { get; protected set; }

	public static string SolutionDirectory { get; protected set; }

	/// <summary>
	/// Represents the Cornerstone test time.
	/// </summary>
	public static DateTime StartDateTime { get; }

	public static string TempDirectory { get; protected set; }

	/// <inheritdoc />
	public DateTime UtcNow => _defaultDateTimeProvider.UtcNow;

	/// <summary>
	/// The timeout to use when waiting for a test state to be hit.
	/// </summary>
	public static TimeSpan WaitTimeout => TimeSpan.FromMilliseconds(IsDebugging ? 1000000 : 1000);

	/// <summary>
	/// Represents test runtime information.
	/// </summary>
	private IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="exclusions"> The settings for the compare session. </param>
	public void AreEqual(object expected, object actual, Func<string> message, params string[] exclusions)
	{
		var settings = new ComparerSettings { GlobalIncludeExcludeSettings = exclusions.ToOnlyExcludingSettings() };
		AreEqual(expected, actual, message, settings);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public void AreEqual(object expected, object actual, TextBuilder message, ComparerSettings? settings = null, Action<CompareSession<object, object>> configure = null)
	{
		AreEqual(expected, actual, () => message?.ToString(), settings, configure);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public void AreEqual(object expected, object actual, string message = null, ComparerSettings? settings = null, Action<CompareSession<object, object>> configure = null)
	{
		AreEqual(expected, actual, () => message, settings, configure);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="includeExcludeSettings"> An optional set of included or excluded properties. </param>
	public void AreEqual<T, T2>(T expected, T2 actual, Func<string> message, IncludeExcludeSettings includeExcludeSettings)
	{
		var settings = new ComparerSettings
		{
			TypeIncludeExcludeSettings =
			{
				{ typeof(T), includeExcludeSettings }
			}
		};
		AreEqual(expected, actual, message, settings);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <typeparam name="T2"> The data type of the actual value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public static void AreEqual<T, T2>(T expected, T2 actual, Func<string> message = null, ComparerSettings? settings = null, Action<CompareSession<T, T2>> configure = null)
	{
		var session = Compare(expected, actual, settings, configure);
		session.Assert(CompareResult.AreEqual, message);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <typeparam name="T2"> The data type of the actual value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public static void AreNotEqual<T, T2>(T expected, T2 actual, string message = null, ComparerSettings? settings = null, Action<CompareSession<T, T2>> configure = null)
	{
		var session = Compare(expected, actual, settings, configure);
		session.Assert(CompareResult.NotEqual, message);
	}

	/// <summary>
	/// Converts type to a file path.
	/// ex. Cornerstone.Babel, C:\Workspace\Cornerstone, .cs
	/// C:\Workspaces\\Cornerstone\\Cornerstone\\Babel.cs
	/// </summary>
	/// <param name="solutionDirectory"> The directory of the solution. </param>
	/// <param name="typeFullName"> The type full name. </param>
	/// <param name="fileExtension"> The file extension. (.ps1, .cs) </param>
	/// <returns> </returns>
	public static string CalculateTypeFilePath(string solutionDirectory, string typeFullName, string fileExtension)
	{
		return $"{solutionDirectory}\\{typeFullName.Replace(".", "\\")}{fileExtension}";
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
	public static CompareSession<T, T2> Compare<T, T2>(T expected, T2 actual, ComparerSettings? settings = null, Action<CompareSession<T, T2>> configure = null)
	{
		var session = Comparer.StartSession(expected, actual, settings);
		configure?.Invoke(session);
		session.Compare();
		return session;
	}

	/// <summary>
	/// Copy the string version of the value to the clipboard.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="value"> The object to copy to the clipboard. Calls ToString on the value. </param>
	/// <returns> The value input to allow for method chaining. </returns>
	[ExcludeFromCodeCoverage]
	public static T CopyToClipboard<T>(T value)
	{
		_clipboard.SetTextAsync(value?.ToString() ?? string.Empty);
		return value;
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

	/// <inheritdoc />
	public Guid GetProviderId()
	{
		return _defaultDateTimeProvider.GetProviderId();
	}

	public UpdateableAction[] GetUpdateableActions()
	{
		_updateableActions ??= EnumExtensions.GetEnumValues(UpdateableAction.None, UpdateableAction.All, UpdateableAction.SyncAll).ToArray();
		return _updateableActions;
	}

	/// <summary>
	/// Get the values for testing. See also <see cref="ValueGenerator.GenerateValues" />.
	/// </summary>
	/// <param name="type"> The type to get values for. </param>
	/// <returns> The values for testing. </returns>
	public virtual IList<object> GetValuesForTesting(Type type)
	{
		var response = ValueGenerator.GenerateValues(type);
		if ((response != null) && type.IsNullable() && !response.Contains(null))
		{
			response = response.Append(null).ToArray();
		}
		return response ?? throw new CornerstoneException($"Type ({type.FullName}) values not found for testing.");
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

		#if NET7_0_OR_GREATER
		if (microseconds != 0)
		{
			currentTime += TimeSpan.FromMicroseconds(microseconds);
		}
		#endif

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
	public void IsFalse([DoesNotReturnIf(true)] bool condition, string message = null)
	{
		if (condition)
		{
			throw new CornerstoneException(message ?? "The condition was incorrectly true and should have been false.");
		}
	}

	/// <summary>
	/// Tests whether the specified condition is not null and throws an exception if the condition is null.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be not null. </param>
	/// <param name="message"> The message is shown in test results. </param>
	public void IsNotNull([NotNull] object condition, string message = null)
	{
		if (condition == null)
		{
			throw new CornerstoneException(message ?? "The condition was incorrectly null and should have been not null.");
		}
	}

	/// <summary>
	/// Tests whether the specified condition is null and throws an exception if the condition is not null.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be null. </param>
	/// <param name="message"> The message is shown in test results. </param>
	public void IsNull(object condition, string message = null)
	{
		IsNull(condition, () => message);
	}

	/// <summary>
	/// Tests whether the specified condition is null and throws an exception if the condition is not null.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be null. </param>
	/// <param name="message"> The message is shown in test results. </param>
	public void IsNull(object condition, Func<string> message)
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
	public void IsTrue([DoesNotReturnIf(false)] bool condition, string message = null)
	{
		if (!condition)
		{
			throw new CornerstoneException(message ?? "The condition was incorrectly false and should have been true.");
		}
	}

	/// <summary>
	/// Validates that the actual is not equal to the expected in any way.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <typeparam name="T2"> The data type of the actual value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public void NothingIsEqual<T, T2>(T expected, T2 actual, Func<string> message = null, ComparerSettings? settings = null, Action<CompareSession<T, T2>> configure = null)
	{
		//var session = Comparer.StartSession(expected, actual, settings);
		//configure?.Invoke(session);
		//session.Compare();
		//session.Assert(CompareResult.AreNotEqual, message?.Invoke());
		throw new NotImplementedException();
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
	/// Sets the clipboard provider.
	/// </summary>
	/// <param name="clipboard"> The provider to be set. </param>
	public void SetClipboardService(IClipboardService clipboard)
	{
		_clipboard = clipboard;
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
		TryToDispose(_clipboard);
	}

	/// <summary>
	/// Initialize the test.
	/// </summary>
	public virtual void TestInitialize()
	{
		#if (WINDOWS)
		SetClipboardService(new WindowsClipboardService(this));
		#endif

		SetupDependencyInjection();
		ResetCurrentTime(StartDateTime);
		Babel.Tower.Reset();
	}

	/// <summary>
	/// Runs a list of items against each other.
	/// </summary>
	/// <typeparam name="T"> The type of the item in the list. </typeparam>
	/// <param name="items"> The items to test. </param>
	/// <param name="action"> The action to run each item against. </param>
	public void TestItemsAgainstEachOther<T>(IEnumerable<T> items, Action<T, T> action)
	{
		var list = items.ToList();

		foreach (var item in list)
		{
			foreach (var otherItem in list)
			{
				if (otherItem.Equals(item))
				{
					continue;
				}

				action(item, otherItem);
			}
		}
	}

	/// <summary>
	/// Throw a <see cref="CornerstoneTest" /> exception.
	/// </summary>
	/// <param name="message"> The error message. </param>
	[DoesNotReturn]
	public void Throw(string message)
	{
		throw new CornerstoneException(message);
	}

	/// <summary>
	/// Time an action to see how long it takes to run.
	/// </summary>
	/// <param name="action"> The action to execute. </param>
	/// <returns> The total elapsed time the action took. </returns>
	public TimeSpan Time(Action action)
	{
		var watch = Stopwatch.StartNew();
		action();
		watch.Stop();
		return watch.Elapsed;
	}

	/// <summary>
	/// Update the provided object with default values.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="model"> The value to update all properties for. </param>
	/// <param name="exclusions"> An optional set of exclusions. </param>
	/// <returns> The type updated with default values. </returns>
	public T UpdateWithDefaultValues<T>(T model, params string[] exclusions)
	{
		return UpdateWithDefaultValues(model, exclusions.ToOnlyExcludingSettings());
	}

	/// <summary>
	/// Update the provided object with default values.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="model"> The value to update all properties for. </param>
	/// <param name="settings"> The include exclude settings. </param>
	/// <returns> The type updated with default values. </returns>
	public T UpdateWithDefaultValues<T>(T model, IncludeExcludeSettings settings)
	{
		var notifiable = model as INotifiable;
		notifiable?.DisablePropertyChangeNotifications();

		try
		{
			var properties = Cache
				.GetSettableProperties(model)
				.Where(x => settings.ShouldProcessProperty(x.Name))
				.OrderBy(x => x.Name)
				.ToList();

			foreach (var property in properties)
			{
				var defaultValue = CreateInstanceOfDefaultValue(property.PropertyType);

				property.SetValue(model, defaultValue);
			}

			return model;
		}
		finally
		{
			notifiable?.EnablePropertyChangeNotifications();
		}
	}

	/// <summary>
	/// Update the provided object with non default values.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="model"> The value to update all properties for. </param>
	/// <param name="exclusions"> An optional set of exclusions. </param>
	/// <returns> The type updated with non default values. </returns>
	public T UpdateWithNonDefaultValues<T>(T model, params string[] exclusions)
	{
		return UpdateWithNonDefaultValues(model, exclusions.ToOnlyExcludingSettings());
	}

	/// <summary>
	/// Update the provided object with non default values.
	/// </summary>
	/// <typeparam name="T"> The type of the value. </typeparam>
	/// <param name="model"> The value to update all properties for. </param>
	/// <param name="settings"> The include exclude settings. </param>
	/// <returns> The type updated with non default values. </returns>
	public virtual T UpdateWithNonDefaultValues<T>(T model, IncludeExcludeSettings settings)
	{
		model.UpdateWithNonDefaultValues(settings, CreateCustomTypeFactory);
		return model;
	}

	/// <summary>
	/// Validate the GetDefaultIncludedProperties member of entities.
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="scenarios"> </param>
	public void ValidateGetDefaultIncludedProperties<T>(Dictionary<UpdateableAction, string[]> scenarios)
		where T : IEntity, new()
	{
		var entity = new T();
		var actual = EnumExtensions
			.GetFlagValues<UpdateableAction>()
			.ToDictionary(x => x, x => entity.GetDefaultIncludedProperties(x).ToArray());

		var missingActions = actual
			.Keys
			.Except(scenarios.Keys)
			.ToList();

		if (missingActions.Any())
		{
			CopyToClipboard(actual.ToCSharp());
			throw new CornerstoneException($"Missing scenarios: {string.Join(",", missingActions)}");
		}

		AreEqual(scenarios, actual, () =>
		{
			var settings = new CodeWriterSettings { TextFormat = TextFormat.Indented };
			CopyToClipboard(actual.ToCSharp(settings)).Dump();
			return "Failed to match expected scenario. Actual scenarios copied to clipboard.";
		});
	}

	/// <summary>
	/// Check to see if the type is supported in the full framework (classic, not core)
	/// </summary>
	/// <param name="type"> The type to be tested. </param>
	/// <returns> True if the type is supported in classic otherwise false. </returns>
	protected bool CheckTypeForClassicFramework(Type type)
	{
		#if (!NETSTANDARD)
		return !(
			(type == typeof(DateOnly))
			|| (type == typeof(DateOnly?))
			|| (type == typeof(TimeOnly))
			|| (type == typeof(TimeOnly?))
		);
		#else
		return true;
		#endif
	}

	protected virtual object CreateCustomTypeFactory(Type type, object[] args)
	{
		return type.CreateInstance(args);
	}

	/// <summary>
	/// Get a non default value for data type.
	/// </summary>
	/// <param name="type"> The type of object to get the value for. </param>
	/// <param name="arguments"> The arguments for the constructing the instance. </param>
	/// <returns> The new instances of the type. </returns>
	protected object CreateInstanceOfDefaultValue(Type type, params object[] arguments)
	{
		if (type.IsDelegate())
		{
			return null;
		}

		return type.IsNullable()
			|| type.IsNullableType()
			|| type.IsInterface
			|| type.IsAbstract
				? null
				: type.CreateInstance(arguments);
	}

	/// <summary>
	/// Get a non default value for data type.
	/// </summary>
	/// <param name="type"> The type of object to get the value for. </param>
	/// <param name="arguments"> The arguments for the constructing the instance. </param>
	/// <returns> The new instances of the type. </returns>
	protected object CreateInstanceOfNonDefaultValue(Type type, params object[] arguments)
	{
		return CreateInstanceOfNonDefaultValue(type, arguments, null);
	}

	/// <summary>
	/// Get a non default value for data type.
	/// </summary>
	/// <param name="type"> The type of object to get the value for. </param>
	/// <param name="requiredRange"> An optional required range. </param>
	/// <param name="arguments"> The arguments for the constructing the instance. </param>
	/// <returns> The new instances of the type. </returns>
	protected object CreateInstanceOfNonDefaultValue(Type type, object[] arguments, RangeAttribute requiredRange)
	{
		return type.CreateInstanceOfNonDefaultValue(arguments, requiredRange);
	}

	/// <summary>
	/// Generate the from and to test strings.
	/// </summary>
	/// <param name="converter"> The converter to process the types. </param>
	/// <param name="fromType"> The [from] type. </param>
	/// <param name="toType"> The [to] type. </param>
	/// <param name="value"> The value to use for conversion. </param>
	/// <param name="settings"> The settings for converting. </param>
	/// <returns> The from and to string. </returns>
	protected static (string from, string to) GenerateConvertTestString(BaseConverter converter, Type fromType, Type toType, object value, IConverterSettings settings = null)
	{
		var from = value.ConvertTo(fromType);

		if (!converter.TryConvertTo(from, fromType, toType, out var to, settings))
		{
			throw new NotSupportedException($"The converter does not support this type.\r\n{fromType.FullName} > {toType.FullName}");
		}

		return (
			CSharpCodeWriter.GenerateCode(from),
			CSharpCodeWriter.GenerateCode(to)
		);
	}

	/// <summary>
	/// Generate the Equals.
	/// </summary>
	protected static TextBuilder GenerateEquals(Type type)
	{
		var builder = new TextBuilder();
		var properties = type
			.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.GetCustomAttribute<ComputedPropertyAttribute>() == null)
			.OrderBy(x => x.Name)
			.ToList();

		builder.AppendLine($$"""
							/// <inheritdoc />
							public bool Equals({{type.GetCodeTypeName()}} state)
							{
								if (state == null)
								{
									return false;
								}
							
								return
							""");

		for (var index = 0; index < properties.Count; index++)
		{
			var p = properties[index];

			builder.AppendLine(
				index < (properties.Count - 1)
					? $"\t\t({p.Name} == state.{p.Name}) &&"
					: $"\t\t({p.Name} == state.{p.Name});"
			);
		}

		builder.AppendLine("}");
		return builder;
	}

	/// <summary>
	/// Generate the GetDefaultIncludedProperties.
	/// </summary>
	protected static StringBuilder GenerateGetDefaultIncludedProperties(Type type)
	{
		var builder = new StringBuilder();
		var syncEntity = new SyncEntity<int>();
		var properties = type
			.GetCachedProperties(BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead && p.CanWrite && !p.IsVirtual())
			.OrderBy(x => x.Name)
			.ToList();

		var propertyLine = "\"" + string.Join("\", \"", properties.Select(x => x.Name)) + "\"";
		builder.AppendLine("""
							/// <inheritdoc />
							public override HashSet<string> GetDefaultIncludedProperties(UpdateableAction action)
							{
								var response = base.GetDefaultIncludedProperties(action);
								
								return action switch
								{
							""");

		var enumDetails = EnumExtensions.GetEnumValues(UpdateableAction.None);

		foreach (var d in enumDetails)
		{
			string section;
			if (d.IsSyncAction())
			{
				var exceptions = syncEntity.GetDefaultIncludedProperties(d);
				if (d == UpdateableAction.SyncIncomingUpdate)
				{
					exceptions.Add(nameof(ISyncEntity.SyncId));
				}

				var syncPropertyLine = "\"" + string.Join("\", \"", properties
					.Where(p => (p.Name != "Id") && !exceptions.Contains(p.Name))
					.Select(x => x.Name)) + "\"";
				section = $"		UpdateableAction.{d} => response.AddRange({syncPropertyLine}),";
			}
			else
			{
				section = $"		UpdateableAction.{d} => response.AddRange({propertyLine}),";
			}

			builder.AppendLine(section);
		}

		builder.AppendLine("""
									_ => response
								};
							}
							""");

		return builder;
	}

	/// <summary>
	/// Generate the HashCode.
	/// </summary>
	protected static TextBuilder GenerateHashCode(Type type)
	{
		var builder = new TextBuilder();
		var properties = type
			.GetCachedProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.GetCustomAttribute<ComputedPropertyAttribute>() == null)
			.OrderBy(x => x.Name)
			.ToList();

		builder.AppendLine("""
							/// <inheritdoc />
							public override int GetHashCode()
							{
								var hashCode = HashCodeCalculator.Combine(
							""");

		builder.PushIndent();
		builder.PushIndent();

		for (var index = 0; index < properties.Count; index++)
		{
			var p = properties[index];

			builder.Append(
				index < (properties.Count - 1)
					? $"{p.Name}, "
					: $"{p.Name}"
			);

			if ((index > 0) && ((index % 4) == 0))
			{
				builder.AppendLine();
			}
		}

		if ((properties.Count % 4) != 0)
		{
			builder.AppendLine();
		}

		builder.PopIndent();
		builder.AppendLine("""
							);
								return hashCode;
							}
							""");
		return builder;
	}

	/// <summary>
	/// Generate the UpdateWith.
	/// </summary>
	protected static TextBuilder GenerateSyncConverters(Type type)
	{
		var typeNamespace = type.Namespace;
		var entities = type.Assembly
			.GetTypes()
			.Where(x =>
				(x.Namespace == typeNamespace)
				&& x.ImplementsType<ISyncEntity>()
				&& !x.IsAbstract
				&& !x.IsInterface
			)
			.ToList();

		var incoming = new TextBuilder();
		var outgoing = new TextBuilder();
		var syncOrder = new TextBuilder();

		foreach (var entity in entities)
		{
			var entityGenerics = entity.BaseType?.GetGenericArguments();
			var syncModel = entityGenerics?.Length > 2
				? entityGenerics[2].Name
				: entity.Name.Replace("Entity", "");

			incoming.AppendLine($"new SyncObjectIncomingConverter<{syncModel}, {entity.Name}>(),");
			outgoing.AppendLine($"new SyncObjectOutgoingConverter<{entity.Name}, {syncModel}>(),");
			syncOrder.AppendLine($"typeof({entity.Name}),");
		}

		incoming.AppendLine();
		incoming.AppendLine();
		incoming.Append(outgoing.ToString());

		incoming.AppendLine();
		incoming.AppendLine();
		incoming.Append(syncOrder.ToString());

		return incoming;
	}

	/// <summary>
	/// Generate the UpdateWith.
	/// </summary>
	protected static TextBuilder GenerateUpdateWith(Type destinationType, params Type[] sourceTypes)
	{
		var builder = new TextBuilder();
		var switchBuilder = new TextBuilder();

		foreach (var sourceType in sourceTypes)
		{
			GenerateMethod(builder, destinationType, sourceType);

			switchBuilder.AppendLine($"{destinationType.GetCodeTypeName()} value => UpdateWith(value, settings),");
		}

		if (builder.Length <= 0)
		{
			return builder;
		}

		builder.AppendLine($@"/// <inheritdoc />
public override bool UpdateWith(object update, IncludeExcludeSettings settings)
{{
	return update switch
	{{
		{switchBuilder.Trim()}
		_ => base.UpdateWith(update, settings)
	}};
}}");

		return builder;
	}

	/// <summary>
	/// Get the best value for testing.
	/// </summary>
	/// <param name="fromType"> The value to convert from. </param>
	/// <param name="toType"> The value to convert to. </param>
	/// <param name="nextDecimal"> The next decimal. </param>
	/// <returns> The value to use for testing. </returns>
	protected object GetBestValueForTesting(Type fromType, Type toType, ref decimal nextDecimal)
	{
		var fromTypeName = CSharpCodeWriter.GetCodeTypeName(fromType);
		var toTypeName = CSharpCodeWriter.GetCodeTypeName(toType);

		if ((nextDecimal % 1) >= 0.49m)
		{
			// Prevent rounding issues for bulk testing, will handle in edge case testing.
			nextDecimal = Math.Truncate(nextDecimal);
		}

		if (nextDecimal >= sbyte.MaxValue)
		{
			nextDecimal = 1;
		}

		if (fromType.IsNullable() && toType.IsNullable())
		{
			return null;
		}

		if (fromType.IsNullable())
		{
			fromType = fromType.FromNullableType();
		}

		if (toType.IsNullable())
		{
			toType = toType.FromNullableType();
		}

		if (Activator.CharTypes.Contains(fromType))
		{
			return RandomGenerator.AllCharacters.Substring((int) nextDecimal % RandomGenerator.AllCharacters.Length, 1)[0];
		}

		if (Activator.BooleanTypes.Contains(fromType))
		{
			if (Activator.BooleanTypes.Contains(toType))
			{
				return (nextDecimal++ % 2) == 0;
			}

			if (Activator.StringTypes.Contains(toType))
			{
				return (nextDecimal++ % 2) == 0 ? "true" : "false";
			}
		}

		if (Activator.StringTypes.Contains(fromType))
		{
			if (Activator.BooleanTypes.Contains(toType))
			{
				return true;
			}

			if (Activator.StringTypes.Contains(toType))
			{
				nextDecimal++;
				return RandomGenerator.AllCharacters.Substring((int) nextDecimal % 16, 16);
			}

			if ((toType == typeof(char)) || (toType == typeof(char?)))
			{
				return RandomGenerator.AllCharacters.Substring((int) nextDecimal % RandomGenerator.AllCharacters.Length, 1)[0];
			}

			if (Activator.NumberTypes.Contains(toType))
			{
				if (Activator.DecimalNumberTypes.Contains(toType))
				{
					nextDecimal += 0.01m;
				}

				nextDecimal++;
				return nextDecimal.ConvertTo(toType);
			}

			if (Activator.DateTypes.Contains(toType))
			{
				var date = StartDateTime.AddSeconds((int) nextDecimal);
				nextDecimal++;
				#if (NETSTANDARD)
				return date;
				#else
				return toType == typeof(DateOnly) ? date.ToDateOnly() : date;
				#endif
			}

			if (Activator.TimeTypes.Contains(toType))
			{
				var date = StartDateTime.AddSeconds((int) nextDecimal);
				nextDecimal++;
				#if (NETSTANDARD)
				return date.TimeOfDay;
				#else
				return toType == typeof(TimeOnly)
					? new TimeOnly(date.TimeOfDay.Ticks)
					: date.TimeOfDay;
				#endif
			}

			if (Activator.GuidTypes.Contains(toType))
			{
				nextDecimal++;
				return toType == typeof(ShortGuid)
					? new ShortGuid(((long) nextDecimal).ToGuid())
					: ((long) nextDecimal).ToGuid();
			}

			if (toType.IsEnum)
			{
				return BestValueForTestingEnum(toType, ref nextDecimal);
			}
		}

		if (Activator.NumberTypes.Contains(fromType)
			|| (fromType == typeof(JsonNumber)))
		{
			if (Activator.NumberTypes.Contains(toType)
				|| (toType == typeof(JsonNumber)))
			{
				if (Activator.DecimalNumberTypes.Contains(fromType)
					&& Activator.DecimalNumberTypes.Contains(toType))
				{
					nextDecimal += 0.01m;
				}

				nextDecimal++;
				return nextDecimal;
			}

			if (Activator.DateTypes.Contains(toType))
			{
				return BestValueForTestingAsDate(fromType, toType, ref nextDecimal);
			}

			if (Activator.GuidTypes.Contains(toType)
				|| Activator.StringTypes.Contains(toType))
			{
				nextDecimal++;
				return nextDecimal.ConvertTo(fromType);
			}

			if (toType.IsEnum)
			{
				return BestValueForTestingEnum(toType, ref nextDecimal);
			}
		}

		if (Activator.GuidTypes.Contains(fromType))
		{
			if (Activator.NumberTypes.Contains(toType))
			{
				if (Activator.DecimalNumberTypes.Contains(fromType)
					&& Activator.DecimalNumberTypes.Contains(toType))
				{
					nextDecimal = Math.Truncate(nextDecimal);
				}
				else if (Activator.DecimalNumberTypes.Contains(fromType)
						|| Activator.DecimalNumberTypes.Contains(toType))
				{
					nextDecimal += 0.01m;
				}

				nextDecimal++;
				return nextDecimal;
			}

			if (Activator.GuidTypes.Contains(toType))
			{
				nextDecimal++;
				return toType == typeof(ShortGuid)
					? new ShortGuid(((long) nextDecimal).ToGuid())
					: ((long) nextDecimal).ToGuid();
			}

			if (Activator.StringTypes.Contains(toType))
			{
				nextDecimal++;
				return fromType == typeof(ShortGuid)
					? new ShortGuid(((long) nextDecimal).ToGuid()).Value
					: ((long) nextDecimal).ToGuid().ToString();
			}
		}

		if (Activator.TimeTypes.Contains(fromType))
		{
			if (Activator.TimeTypes.Contains(toType))
			{
				nextDecimal++;
				return TimeSpan.FromSeconds((long) nextDecimal);
			}

			if (Activator.StringTypes.Contains(toType))
			{
				return TimeSpan.FromSeconds((long) nextDecimal).ConvertTo(toType);
			}
		}

		if (Activator.DateTypes.Contains(fromType))
		{
			if (Activator.DateTypes.Contains(toType))
			{
				return BestValueForTestingAsDate(fromType, toType, ref nextDecimal);
			}

			if (Activator.StringTypes.Contains(toType))
			{
				return BestValueForTestingAsDate(fromType, toType, ref nextDecimal).ConvertTo(toType);
			}
		}

		if (fromType.IsEnum)
		{
			return BestValueForTestingEnum(fromType, ref nextDecimal);
		}

		throw new($"{fromTypeName} -> {toTypeName} missing");
	}

	/// <summary>
	/// Create a new instance of the type then update the object with non default values.
	/// </summary>
	/// <param name="type"> The type to create. </param>
	/// <param name="exclusions"> An optional set of exclusions. </param>
	/// <returns> The instance of the type with non default values. </returns>
	protected object GetModelWithDefaultValues(Type type, params string[] exclusions)
	{
		return GetModelWithDefaultValues(type, exclusions.ToOnlyExcludingSettings());
	}

	/// <summary>
	/// Create a new instance of the type then update the object with non default values.
	/// </summary>
	/// <param name="type"> The type to create. </param>
	/// <param name="settings"> The include exclude settings. </param>
	/// <returns> The instance of the type with non default values. </returns>
	protected virtual object GetModelWithDefaultValues(Type type, IncludeExcludeSettings settings)
	{
		var response = CreateInstanceOfNonDefaultValue(type);
		UpdateWithDefaultValues(response, settings);
		return response;
	}

	/// <summary>
	/// Create a new instance of the type then update the object with non default values.
	/// </summary>
	/// <param name="type"> The type to create. </param>
	/// <param name="exclusions"> An optional set of exclusions. </param>
	/// <returns> The instance of the type with non default values. </returns>
	protected object GetModelWithNonDefaultValues(Type type, params string[] exclusions)
	{
		return GetModelWithDefaultValues(type, exclusions.ToOnlyExcludingSettings());
	}

	/// <summary>
	/// Create a new instance of the type then update the object with non default values.
	/// </summary>
	/// <param name="type"> The type to create. </param>
	/// <param name="settings"> The include exclude settings. </param>
	/// <returns> The instance of the type with non default values. </returns>
	protected virtual object GetModelWithNonDefaultValues(Type type, IncludeExcludeSettings settings)
	{
		var response = CreateInstanceOfNonDefaultValue(type);
		UpdateWithNonDefaultValues(response, settings);
		return response;
	}

	/// <summary>
	/// Will try to find file based on type full path.
	/// </summary>
	/// <param name="shouldProcess"> Must be true to update file. </param>
	/// <param name="destination"> The type to try and process. </param>
	/// <param name="source"> The source to update. </param>
	/// <param name="solutionPath"> The solution path. </param>
	protected void RefreshCodeGeneratedUpdateWith(bool shouldProcess, Type destination, Type source, string solutionPath)
	{
		if (!shouldProcess)
		{
			return;
		}

		var filePath = CalculateTypeFilePath(solutionPath, destination.FullName, ".cs");
		if (!File.Exists(filePath))
		{
			return;
		}

		var startComment = string.Format(CSharpCodeWriter.SlashSectionFormat, $"UpdateWith - {source.Name}");
		var endComment = string.Format(CSharpCodeWriter.SlashSectionFormat, $"/UpdateWith - {source.Name}");

		if (TryGetPropertiesToUpdate(destination, source, out var properties, out var isOverload))
		{
			return;
		}

		var builder = new TextBuilder();
		builder.PushIndent();
		builder.PushIndent();
		GenerateUpdateWith(builder, properties, startComment, endComment);
		var newContent = builder.ToString();

		var content = new TextBuilder(File.ReadAllText(filePath));
		var startIndex = content.IndexOf(startComment);
		var endIndex = content.IndexOf(endComment);

		if ((startIndex == -1) || (endIndex == -1))
		{
			//Debugger.Break();
			return;
		}

		endIndex += endComment.Length;

		content.Replace(startIndex, endIndex - startIndex, newContent);

		File.WriteAllText(filePath, content.ToString(), Encoding.Default);
	}

	/// <summary>
	/// Setup the default services.
	/// </summary>
	protected virtual void SetupDependencyInjection()
	{
		SetupCornerstoneServices(
			_defaultDateTimeProvider,
			RuntimeInformation
		);
	}

	protected void UpdateableShouldUpdateAll(bool updateCodeGeneratedFiles, Type updateableType, Assembly[] assemblies,
		List<Type> exclusions, ComparerSettings settings, Func<Type, bool> additionalTypeFilter = null)
	{
		var types = assemblies
			.SelectMany(s => s.GetTypes())
			.Where(t => !exclusions.Contains(t))
			.Where(t => (t != null) && updateableType.IsAssignableFrom(t))
			// Uncommit to test a single type
			.Where(additionalTypeFilter ?? (_ => true))
			.Where(x =>
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

				return x.GetCachedMethods()
					.Any(m => m.Name == nameof(IUpdateable.UpdateWith));
			})
			.ToArray();

		var updateableSourceTypes = types
			.ToDictionary(
				x => x,
				x => x
					.GetInterfaces()
					.Where(m =>
						m.IsDirectDescendantOf(typeof(IUpdateable))
						&& (m.GenericTypeArguments.Length > 0)
					)
					.Select(t => t.GenericTypeArguments[0])
					.Distinct()
					.ToList()
			)
			.Where(x => x.Value.Count > 0)
			.ToDictionary(x => x.Key, x => x.Value);

		foreach (var destinationType in types)
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
				typesToTest.Add(additional);
			}

			foreach (var sourceType in typesToTest)
			{
				if (sourceType != destinationType)
				{
					$"\t> {sourceType.FullName}".Dump();
				}

				RefreshCodeGeneratedUpdateWith(updateCodeGeneratedFiles,
					destinationType, sourceType, SolutionDirectory
				);

				var destinationWithDefaults = GetModelWithDefaultValues(destinationType, includeExcludeSettings);
				if (destinationWithDefaults is IRequiresDateTimeProvider requiresWithDefaults)
				{
					requiresWithDefaults.UpdateDateTimeProvider(this);
				}

				var destinationWithNonDefaults = GetModelWithNonDefaultValues(destinationType, includeExcludeSettings);
				if (destinationWithNonDefaults is IRequiresDateTimeProvider requiresNonDefault)
				{
					requiresNonDefault.UpdateDateTimeProvider(this);
				}

				ValidateUnwrap(destinationWithNonDefaults, settings);
				ValidateAllValuesAreNotDefault(destinationWithNonDefaults, includeExcludeSettings);

				ValidateUpdateWith(destinationType, destinationType, destinationWithDefaults, settings);
				ValidateUpdateWith(destinationType, destinationType, destinationWithNonDefaults, settings);

				if (sourceType == destinationType)
				{
					continue;
				}

				var sourceWithDefaults = GetModelWithDefaultValues(sourceType, includeExcludeSettings);
				if (sourceWithDefaults is IRequiresDateTimeProvider requiresWithDefaults2)
				{
					requiresWithDefaults2.UpdateDateTimeProvider(this);
				}

				var sourceWithNonDefaults = GetModelWithNonDefaultValues(sourceType, includeExcludeSettings);
				if (sourceWithNonDefaults is IRequiresDateTimeProvider requiresNonDefault2)
				{
					requiresNonDefault2.UpdateDateTimeProvider(this);
				}

				ValidateUpdateWith(destinationType, sourceType, sourceWithDefaults, settings);
				ValidateUpdateWith(destinationType, sourceType, sourceWithNonDefaults, settings);
			}
		}
	}

	/// <summary>
	/// </summary>
	/// <param name="filePath"> </param>
	/// <param name="code"> </param>
	/// <param name="indent"> </param>
	/// <exception cref="InvalidDataException"> </exception>
	/// <exception cref="Exception"> </exception>
	protected void UpdateFileIfNecessary(string filePath, ITextBuilder code, string indent = "\t\t\t")
	{
		var startToken = "// <Scenarios>\r\n";
		var endToken = "// </Scenarios>";
		FileModifier.UpdateFileIfNecessary(startToken, endToken, filePath, code.ToString(), indent);
	}

	/// <summary>
	/// Validates that all values are not default value.
	/// </summary>
	/// <typeparam name="T"> The type of the model. </typeparam>
	/// <param name="model"> The model to be validated. </param>
	/// <param name="settings"> The include exclude settings. </param>
	protected void ValidateAllValuesAreNotDefault<T>(T model, IncludeExcludeSettings settings)
	{
		var properties = Cache
			.GetSettableProperties(model)
			.Where(x => settings.ShouldProcessProperty(x.Name))
			.ToList();

		foreach (var property in properties)
		{
			var propertyValue = property.GetValue(model);
			var defaultValue = CreateInstanceOfDefaultValue(property.PropertyType);

			if (propertyValue?.Equals(defaultValue) == true)
			{
				continue;
			}

			if (Equals(propertyValue, defaultValue))
			{
				throw new Exception($"Property {model.GetType().Name}.{property.Name} matches default but shouldn't be.");
			}
		}
	}

	/// <summary>
	/// Validate all types to ensure the type has the correct members for serializing.
	/// </summary>
	/// <param name="types"> The types to be validated. </param>
	protected void ValidateSerializedModelsMembers(IEnumerable<Type> types)
	{
		var failed = false;

		foreach (var type in types)
		{
			type.FullName.Dump();

			var attribute = type.GetCustomAttribute<SerializedModelAttribute>();
			var attributeExclusions = type.GetCustomAttribute<SerializedModelExclusionsAttribute>();

			IsNotNull(attribute);

			var properties = type.GetCachedProperties()
				.Select(x => x.Name)
				.ToHashSet();

			if (attributeExclusions != null)
			{
				properties = properties
					.Except(attributeExclusions.Exclusions)
					.ToHashSet();
			}

			var expectedProperties = attribute?.Expected.ToHashSet();
			var missingProperties = properties.Except(expectedProperties).ToList();
			var missingExpected = expectedProperties.Except(properties).ToList();

			if (missingProperties.Any() || missingExpected.Any())
			{
				if (missingProperties.Any())
				{
					"Missing on Attribute: ".Dump();
					$"\"{string.Join("\", \"", missingProperties)}\"".Dump();
				}

				if (missingExpected.Any())
				{
					"Missing on Object: ".Dump();
					$"\"{string.Join("\", \"", missingExpected)}\"".Dump();
				}

				type.FullName.Dump();
				$"[SerializedModel(\"{string.Join("\", \"", properties)}\")]".Dump();

				failed = true;
			}
		}

		if (failed)
		{
			Fail("Missing something... see output.");
		}
	}

	/// <summary>
	/// Validate a model that is "IUnwrappable"
	/// </summary>
	/// <param name="model"> The model to test. </param>
	/// <param name="settings"> The settings. </param>
	protected void ValidateUnwrap(object model, ComparerSettings settings)
	{
		var type = model.GetType();
		var actual = model.Unwrap();

		if (actual is IRequiresDateTimeProvider requires)
		{
			requires.UpdateDateTimeProvider(this);
		}

		var unwrapSettings = Cache.GetSettings(type, UpdateableAction.UnwrapProxyEntity);
		var comparerSettings = settings.ShallowClone();
		comparerSettings.TypeIncludeExcludeSettings.AddOrUpdate(type, unwrapSettings);

		AreEqual(model, actual,
			() =>
			{
				var actualType = actual.GetType();
				var builder = GenerateUpdateWith(actualType, actualType);
				CopyToClipboard(builder);
				builder.Dump();
				return model.GetType().FullName;
			},
			comparerSettings
		);
	}

	/// <summary>
	/// Validate a model's UpdateWith using the "IUpdateable" interface.
	/// </summary>
	/// <param name="destinationType"> The type of the destination. </param>
	/// <param name="sourceType"> The type of the source. </param>
	/// <param name="source"> The source update to apply to the destination. </param>
	/// <param name="comparerSettings"> The setting for comparing. </param>
	protected void ValidateUpdateWith(Type destinationType, Type sourceType, object source, ComparerSettings comparerSettings)
	{
		var actions = GetUpdateableActions();

		foreach (var action in actions)
		{
			var destinationInstance = destinationType.CreateInstance();

			IsNotNull(destinationInstance);

			if (destinationInstance is IRequiresDateTimeProvider requires)
			{
				requires.UpdateDateTimeProvider(this);
			}

			var destination = (IUpdateable) destinationInstance;
			var settings = Cache.GetSettings(destinationType, action);
			destination.UpdateWith(source, settings);

			var areEqualSettings = comparerSettings.ShallowClone();

			if ((settings.ExcludedProperties.Count > 0)
				&& (settings.IncludedProperties.Count <= 0))
			{
				// todo: what could we check?
				// Everything is excluded and nothing is included, aka nothing to check
				return;
			}
			
			areEqualSettings.TypeIncludeExcludeSettings.AddOrUpdate(destinationType, () => settings, x => x.WithMoreSettings(settings));

			AreEqual(destination, source,
				() =>
				{
					$"\t > {action}".Dump();

					CopyToClipboard(GenerateUpdateWith(destinationType, sourceType)).Dump();
					return destinationType.FullName;
				},
				areEqualSettings
			);
		}
	}

	private static object BestValueForTestingAsDate(Type fromType, Type toType, ref decimal nextDecimal)
	{
		nextDecimal++;
		#if NETSTANDARD
		return StartDateTime
			.AddSeconds((long) nextDecimal)
			.AddMilliseconds((long) nextDecimal);
		#else
		if ((fromType == typeof(DateOnly)) || (toType == typeof(DateOnly)))
		{
			return new DateOnly((int) (2000 + nextDecimal), 1, 1);
		}
		return StartDateTime
			.AddSeconds((long) nextDecimal)
			.AddMilliseconds((long) nextDecimal)
			.AddMilliseconds((long) nextDecimal);
		#endif
	}

	private static object BestValueForTestingEnum(Type fromType, ref decimal nextDecimal)
	{
		var d = EnumExtensions.GetAllEnumDetails(fromType);
		var o = (int) (nextDecimal++ % d.Count);
		return d.Values.ToList()[o].Value;
	}

	private static void GenerateMethod(TextBuilder builder, Type destinationType, Type sourceType)
	{
		if (TryGetPropertiesToUpdate(destinationType, sourceType, out var properties, out var isOverload))
		{
			return;
		}

		builder.AppendLine($@"/// <summary>
/// Update the {destinationType.Name.Replace("`1", "").Replace("`2", "")} with an update.
/// </summary>
/// <param name=""update""> The update to be applied. </param>
/// <param name=""settings""> The settings for controlling the updating of the entity. </param>
public {(isOverload ? "override" : "virtual")} bool UpdateWith({sourceType.GetCodeTypeName()} update, IncludeExcludeSettings settings)
{{");
		builder.PushIndent();
		var startComment = string.Format(CSharpCodeWriter.SlashSectionFormat, $"UpdateWith - {sourceType.Name}");
		var endComment = string.Format(CSharpCodeWriter.SlashSectionFormat, $"/UpdateWith - {sourceType.Name}");
		GenerateUpdateWith(builder, properties, startComment, endComment);
		builder.AppendLine();
		builder.AppendLine();
		builder.AppendLine(isOverload ? "return true;" : "return base.UpdateWith(update, settings);");
		builder.PopIndent();
		builder.AppendLine("}");
		builder.AppendLine();
	}

	private static void GenerateUpdateWith(TextBuilder builder, List<PropertyInfo> properties,
		string startComment, string endComment)
	{
		builder.AppendLine(startComment);
		builder.AppendLine();
		builder.AppendLine("// If the update is null then there is nothing to do.");
		builder.AppendLine("if (update == null)");
		builder.AppendLine("{");
		builder.AppendLine("\treturn false;");
		builder.AppendLine("}");
		builder.AppendLine();
		builder.AppendLine("// ****** This code has been auto generated, do not edit this. ******");
		builder.AppendLine();

		foreach (var p in properties)
		{
			UpdateWithAppendSetProperty(p, builder);
		}

		builder.AppendLine();
		builder.Append(endComment);
	}

	private static bool IsDirectInheritingCornerstoneType(Type type)
	{
		return type.IsDirectDescendantOf(typeof(Bindable))
			|| type.IsDirectDescendantOf(typeof(Bindable<>))
			|| type.IsDirectDescendantOf(typeof(Notifiable))
			|| type.IsDirectDescendantOf(typeof(Notifiable<>))
			|| type.IsDirectDescendantOf(typeof(SyncEntity<>))
			|| type.IsDirectDescendantOf(typeof(SyncEntity<,>))
			|| type.IsDirectDescendantOf(typeof(ClientSyncEntity<>))
			|| type.IsDirectDescendantOf(typeof(ClientSyncEntity<,>))
			|| type.IsDirectDescendantOf(typeof(CreatedEntity<>))
			|| type.IsDirectDescendantOf(typeof(ModifiableEntity<>))
			|| type.IsDirectDescendantOf(typeof(Entity))
			|| type.IsDirectDescendantOf(typeof(Entity<>))
			|| type.IsDirectDescendantOf(typeof(SettingsFile<>));
	}

	private static bool TryGetPropertiesToUpdate(Type destination, Type source, out List<PropertyInfo> properties, out bool isOverload)
	{
		isOverload = IsDirectInheritingCornerstoneType(destination);

		var isLastClass = isOverload || (destination != source);
		var destinationProperties = destination
			.GetCachedProperties(
				isLastClass
					? BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance
					: BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance
			)
			.Where(x => x.GetCustomAttribute<ComputedPropertyAttribute>() == null)
			.OrderBy(x => x.Name)
			.ToList();

		isLastClass = IsDirectInheritingCornerstoneType(source) || (destination != source);
		var sourceProperties = source
			.GetCachedProperties(
				isLastClass
					? BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance
					: BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance
			)
			.Where(x => x.GetCustomAttribute<ComputedPropertyAttribute>() == null)
			.OrderBy(x => x.Name)
			.ToList();

		properties = destinationProperties
			.Where(x => sourceProperties.Any(s => s.Name == x.Name)
				&& (x.CanWrite || x.PropertyType.ImplementsType<IUpdateable>())
			)
			.ToList();

		return properties.Count == 0;
	}

	private void TryToDispose<T>(T value)
	{
		if (value is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	private static bool UpdateWithAppendSetProperty(PropertyInfo p, TextBuilder builder)
	{
		//p.Name.Dump();

		if (p.GetCustomAttribute<IgnoreUpdateWithAttribute>() != null)
		{
			return false;
		}

		if (!p.CanWrite)
		{
			if (p.PropertyType.ImplementsType<IUpdateable>())
			{
				builder.Append($"UpdateProperty({p.Name}, update.{p.Name}, ");
				builder.Append($"settings.ShouldProcessProperty(nameof({p.Name}))");
				builder.AppendLine(");");
				return true;
			}

			return false;
		}

		builder.Append($"UpdateProperty({p.Name}, update.{p.Name}, ");
		builder.Append($"settings.ShouldProcessProperty(nameof({p.Name})), ");
		builder.Append($"x => {p.Name} = x");
		builder.AppendLine(");");
		return true;
	}

	#endregion
}