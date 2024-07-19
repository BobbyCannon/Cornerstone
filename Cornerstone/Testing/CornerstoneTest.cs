#region References

using System;
using System.Collections.Generic;
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
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Settings;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Text;
using TimeProvider = Cornerstone.Runtime.TimeProvider;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// The base test for the Cornerstone framework.
/// </summary>
public abstract partial class CornerstoneTest : ITimeProvider
{
	#region Fields

	private static IClipboardService _clipboard;
	private DateTime? _currentDateTime;
	private readonly ITimeProvider _timeService;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the Cornerstone test.
	/// </summary>
	protected CornerstoneTest()
	{
		_timeService = new TimeProvider(
			Guid.Parse("9C20125A-8E7A-428C-BA1C-9BDB72B7A543"),
			() => _currentDateTime ?? DateTime.UtcNow
		);
		_currentDateTime = StartDateTime;

		RuntimeInformation = new RuntimeInformation();
	}

	static CornerstoneTest()
	{
		// Default start date
		StartDateTime = new DateTime(2000, 01, 02, 03, 04, 00, DateTimeKind.Utc);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Returns true if the debugger is attached.
	/// </summary>
	public static bool IsDebugging => Debugger.IsAttached;

	/// <inheritdoc />
	public DateTime Now => _timeService.Now;

	/// <summary>
	/// Represents test runtime information.
	/// </summary>
	public IRuntimeInformation RuntimeInformation { get; }

	/// <summary>
	/// Represents the Cornerstone test time.
	/// </summary>
	public static DateTime StartDateTime { get; set; }

	/// <inheritdoc />
	public DateTime UtcNow => _timeService.UtcNow;

	/// <summary>
	/// The timeout to use when waiting for a test state to be hit.
	/// </summary>
	public static TimeSpan WaitTimeout => TimeSpan.FromMilliseconds(IsDebugging ? 1000000 : 1000);

	#endregion

	#region Methods

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="options"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public void AreEqual(object expected, object actual, TextBuilder message, ComparerOptions? options = null, Action<CompareSession<object, object>> configure = null)
	{
		AreEqual(expected, actual, () => message?.ToString(), options, configure);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="options"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public void AreEqual(object expected, object actual, string message = null, ComparerOptions? options = null, Action<CompareSession<object, object>> configure = null)
	{
		AreEqual(expected, actual, () => message, options, configure);
	}

	/// <summary>
	/// Validates that the actual is equal to the expected. If they are not equal a <see cref="CompareException" /> is thrown.
	/// </summary>
	/// <typeparam name="T"> The data type of the expected value. </typeparam>
	/// <typeparam name="T2"> The data type of the actual value. </typeparam>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="message"> An optional prefix to include with the assert message. </param>
	/// <param name="options"> The settings for the compare session. </param>
	/// <param name="configure"> Optional configuration before the session processes. </param>
	public static void AreEqual<T, T2>(T expected, T2 actual, Func<string> message = null, ComparerOptions? options = null, Action<CompareSession<T, T2>> configure = null)
	{
		var session = Comparer.StartSession(expected, actual, options);
		configure?.Invoke(session);
		session.Compare();
		session.Assert(CompareResult.AreEqual, message?.Invoke());
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
	public void AreNotEqual<T, T2>(T expected, T2 actual, string message = null, ComparerOptions? settings = null, Action<CompareSession<T, T2>> configure = null)
	{
		var session = Comparer.StartSession(expected, actual, settings);
		configure?.Invoke(session);
		session.Compare();
		session.Assert(CompareResult.NotEqual, message);
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
	#if (NET6_0_OR_GREATER)
	[DoesNotReturn]
	#endif
	public static void Fail(string message)
	{
		throw new CornerstoneException(message);
	}

	/// <inheritdoc />
	public Guid GetProviderId()
	{
		return _timeService.GetProviderId();
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
		var currentTime = _timeService.UtcNow;

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
	public void IncrementTime(TimeSpan value)
	{
		var provider = _timeService;
		if (provider == null)
		{
			return;
		}

		var currentTime = provider.UtcNow;

		SetTime(currentTime + value);
	}

	/// <summary>
	/// Tests whether the specified condition is false and throws an exception if the condition is true.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be false. </param>
	/// <param name="message"> The message is shown in test results. </param>
	#if (NET6_0_OR_GREATER)
	public void IsFalse([DoesNotReturnIf(true)] bool condition, string message = null)
	#else
	public void IsFalse(bool condition, string message = null)
		#endif
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
	#if (NET6_0_OR_GREATER)
	public void IsNotNull([NotNull] object condition, string message = null)
	#else
	public void IsNotNull(object condition, string message = null)
		#endif
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
		if (condition != null)
		{
			throw new CornerstoneException(message ?? "The condition was incorrectly not null and should have been null.");
		}
	}

	/// <summary>
	/// Tests whether the specified condition is true and throws an exception if the condition is false.
	/// </summary>
	/// <param name="condition"> The condition the test expects to be true. </param>
	/// <param name="message"> The message is shown in test results. </param>
	#if (NET6_0_OR_GREATER)
	public void IsTrue([DoesNotReturnIf(false)] bool condition, string message = null)
	#else
	public void IsTrue(bool condition, string message = null)
		#endif
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
	public void NothingIsEqual<T, T2>(T expected, T2 actual, Func<string> message = null, ComparerOptions? settings = null, Action<CompareSession<T, T2>> configure = null)
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

		TimeService.Reset(this);
	}

	/// <summary>
	/// Sets the clipboard provider.
	/// </summary>
	/// <param name="clipboard"> The provider to be set. </param>
	public static void SetClipboardService(IClipboardService clipboard)
	{
		_clipboard = clipboard;
	}

	/// <summary>
	/// Set the time to a specific value.
	/// </summary>
	/// <param name="value"> The new time to set. </param>
	public DateTime SetTime(DateTime value)
	{
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
		// Reset the current time back to the start time.
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
	#if (NET6_0_OR_GREATER)
	[DoesNotReturn]
	#endif
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
	/// Validate the GetDefaultIncludedProperties member of entities.
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="scenarios"> </param>
	public void ValidateGetDefaultIncludedProperties<T>(Dictionary<UpdateableAction, string[]> scenarios)
		where T : IEntity, new()
	{
		var entity = new T();

		var missingActions = EnumExtensions
			.GetEnumValues<UpdateableAction>()
			.Except(scenarios.Keys)
			.ToList();

		if (missingActions.Any())
		{
			throw new CornerstoneException($"Missing scenarios: {string.Join(",", missingActions)}");
		}

		foreach (var scenario in scenarios)
		{
			var actual = entity.GetDefaultIncludedProperties(scenario.Key);
			AreEqual(scenario.Value, actual, $"{scenario.Key}: \"{string.Join("\", \"", actual)}\"");
		}
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

	/// <summary>
	/// Configure the dependencies for the tests.
	/// </summary>
	protected virtual void ConfigureDependencies()
	{
	}

	/// <summary>
	/// Generate the from and to test strings.
	/// </summary>
	/// <param name="converter"> The converter to process the types. </param>
	/// <param name="fromType"> The [from] type. </param>
	/// <param name="toType"> The [to] type. </param>
	/// <param name="value"> The value to use for conversion. </param>
	/// <param name="options"> The options for converting. </param>
	/// <returns> The from and to string. </returns>
	protected static (string from, string to) GenerateConvertTestString(BaseConverter converter, Type fromType, Type toType, object value, IConverterOptions options = null)
	{
		var from = value.ConvertTo(fromType);

		if (!converter.TryConvertTo(from, fromType, toType, out var to, options))
		{
			throw new NotSupportedException("The converter does not support this type.");
		}

		return (
			CSharpCodeWriter.GenerateCode(from),
			CSharpCodeWriter.GenerateCode(to)
		);
	}

	/// <summary>
	/// Generate the UpdateWith.
	/// </summary>
	protected static StringBuilder GenerateUpdateWith(Type type)
	{
		var builder = new StringBuilder();
		var declaredOnly = !type.IsDirectDescendantOf(typeof(Bindable))
			&& !type.IsDirectDescendantOf(typeof(Bindable<>))
			&& !type.IsDirectDescendantOf(typeof(Notifiable))
			&& !type.IsDirectDescendantOf(typeof(Notifiable<>))
			&& !type.IsDirectDescendantOf(typeof(SyncEntity<>))
			&& !type.IsDirectDescendantOf(typeof(SyncEntity<>))
			&& !type.IsDirectDescendantOf(typeof(CreatedEntity<>))
			&& !type.IsDirectDescendantOf(typeof(ModifiableEntity<>))
			&& !type.IsDirectDescendantOf(typeof(Entity<>))
			&& !type.IsDirectDescendantOf(typeof(SettingsFile<>));

		// todo: support finding "last implemented" parent of [UpdateWith]

		var properties = type
			.GetCachedProperties(
				declaredOnly
					? BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance
					: BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance
			)
			.Where(x => x.GetCustomAttribute<ComputedPropertyAttribute>() == null)
			.OrderBy(x => x.Name)
			.ToList();

		builder.AppendLine($@"/// <summary>
/// Update the {type.Name.Replace("`1", "").Replace("`2", "")} with an update.
/// </summary>
/// <param name=""update""> The update to be applied. </param>
/// <param name=""options""> The options for controlling the updating of the entity. </param>
public override bool UpdateWith({type.ToCSharpCode()} update, IncludeExcludeOptions options)
{{");
		builder.AppendLine("\t// If the update is null then there is nothing to do.");
		builder.AppendLine("\tif (update == null)\r\n\t{\r\n\t\treturn false;\r\n\t}\r\n");
		builder.AppendLine("\t// ****** You can use GenerateUpdateWith to update this ******");
		builder.AppendLine();
		builder.AppendLine("\tif ((options == null) || options.IsEmpty())");
		builder.AppendLine("\t{");

		bool isGenericList(Type typeToCheck)
		{
			return typeToCheck.IsGenericType
				&& typeToCheck.GetGenericTypeDefinition().IsEnumerable();
		}

		foreach (var p in properties)
		{
			//p.Name.Dump();

			if (p.GetCustomAttribute<IgnoreUpdateWithAttribute>() != null)
			{
				continue;
			}

			if (p.PropertyType.IsArray && p.CanWrite)
			{
				builder.AppendLine($"\t\t{p.Name} = update.{p.Name}.ToArray();");
				continue;
			}

			if (isGenericList(p.PropertyType))
			{
				builder.AppendLine($"\t\t{p.Name}.Reconcile(update.{p.Name});");
				continue;
			}

			if (p.PropertyType.ImplementsType<IUpdateable>())
			{
				builder.AppendLine($"\t\t{p.Name}.UpdateWith(update.{p.Name});");
				continue;
			}

			if (p.PropertyType.ImplementsType<IUpdateable>())
			{
				builder.AppendLine($"\t\t{p.Name}.UpdateWith(update.{p.Name});");
				continue;
			}

			if (!p.CanWrite)
			{
				continue;
			}

			builder.AppendLine($"\t\t{p.Name} = update.{p.Name};");
		}

		builder.AppendLine("\t}");
		builder.AppendLine("\telse");
		builder.AppendLine("\t{");

		foreach (var p in properties)
		{
			if (p.GetCustomAttribute<IgnoreUpdateWithAttribute>() != null)
			{
				continue;
			}

			if (p.PropertyType.IsArray && p.CanWrite)
			{
				builder.AppendLine($"\t\tthis.IfThen(_ => options.ShouldProcessProperty(nameof({p.Name})), x => x.{p.Name} = update.{p.Name});");
				continue;
			}

			if (isGenericList(p.PropertyType))
			{
				builder.AppendLine($"\t\tthis.IfThen(_ => options.ShouldProcessProperty(nameof({p.Name})), x => x.{p.Name}.Reconcile(update.{p.Name}));");
				continue;
			}

			if (p.PropertyType.ImplementsType<IUpdateable>())
			{
				builder.AppendLine($"\t\tthis.IfThen(_ => options.ShouldProcessProperty(nameof({p.Name})), x => x.{p.Name}.UpdateWith(update.{p.Name}));");
				continue;
			}

			if (!p.CanWrite)
			{
				continue;
			}

			builder.AppendLine($"\t\tthis.IfThen(_ => options.ShouldProcessProperty(nameof({p.Name})), x => x.{p.Name} = update.{p.Name});");
		}

		builder.AppendLine("\t}\r\n");
		builder.AppendLine(declaredOnly ? "\treturn base.UpdateWith(update, options);" : "\treturn true;");
		builder.AppendLine("}");

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
		UpdateFileIfNecessary(startToken, endToken, filePath, code.ToString(), indent);
	}

	/// <summary>
	/// </summary>
	/// <param name="startToken"> </param>
	/// <param name="endToken"> </param>
	/// <param name="filePath"> </param>
	/// <param name="code"> </param>
	/// <param name="indent"> </param>
	/// <exception cref="InvalidDataException"> </exception>
	/// <exception cref="Exception"> </exception>
	protected void UpdateFileIfNecessary(string startToken, string endToken, string filePath, string code, string indent = "\t\t\t")
	{
		if (!File.Exists(filePath))
		{
			throw new InvalidDataException("File path is incorrect...");
		}

		var fileData = File.ReadAllText(filePath);
		var startIndex = fileData.IndexOf(startToken);
		if (startIndex == -1)
		{
			throw new InvalidDataException("Failed to find start token...");
		}

		startIndex += startToken.Length;
		var endIndex = fileData.IndexOf(endToken, startIndex);
		if ((endIndex == -1) || (endIndex <= startIndex))
		{
			throw new InvalidDataException("Failed to find end token...");
		}

		var currentScenarios = fileData.Substring(startIndex, endIndex - startIndex);

		// Remove any white spaces
		var newScenarios = code.Trim();

		// Remove last ',' comma.
		if (code.EndsWith(","))
		{
			newScenarios = newScenarios.Substring(0, newScenarios.Length - 1);
		}

		// Add indentation to new scenarios
		newScenarios = string.Join(Environment.NewLine,
			newScenarios
				.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
				.Select(x => $"{indent}{x}")
		);
		newScenarios += Environment.NewLine + indent;

		if (string.Equals(currentScenarios, newScenarios))
		{
			// nothing needs to change
			return;
		}

		// Remove old tests
		fileData = fileData.Remove(startIndex, endIndex - startIndex);
		fileData = fileData.Insert(startIndex, newScenarios);

		// Save the file
		File.WriteAllText(filePath, fileData);
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

			var expectedProperties = attribute.Expected.ToHashSet();
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

	#endregion
}