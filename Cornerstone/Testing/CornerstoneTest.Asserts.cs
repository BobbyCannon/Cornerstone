#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Compare;

#endregion

namespace Cornerstone.Testing;

public abstract partial class CornerstoneTest
{
	#region Methods

	public static void AreEqual<T>(T expected, T actual, IEqualityComparer<T> comparer, Func<string> message = null)
	{
		if (comparer == null)
		{
			AreEqual(expected, actual, message);
			return;
		}

		if (!comparer.Equals(expected, actual))
		{
			Fail(message?.Invoke() ?? "Values are not equal.");
		}
	}

	public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer, Func<string> message = null)
	{
		if (comparer == null)
		{
			AreEqual(expected, actual, message);
			return;
		}

		var expectedList = expected.ToList();
		var actualList = actual.ToList();
		if (expectedList.Count != actualList.Count)
		{
			Fail(message?.Invoke() ?? $"Expected {expectedList.Count} items but found {actualList.Count}.");
		}

		for (var i = 0; i < expectedList.Count; i++)
		{
			if (!comparer.Equals(expectedList[i], actualList[i]))
			{
				Fail(message?.Invoke() ?? $"Values are not equal at index {i}.");
			}
		}
	}

	public static void AreEqual(string expected, string actual, bool ignoreCase, Func<string> message = null)
	{
		AreEqual(expected, actual, message, new ComparerSettings
		{
			StringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
		});
	}

	public static void AreEqual(double expected, double actual, int precision, Func<string> message = null)
	{
		AreEqual(expected, actual, Math.Pow(10, -precision), message);
	}

	public static void AreEqual(double expected, double actual, double precision, Func<string> message = null)
	{
		if (Math.Abs(expected - actual) > precision)
		{
			Fail(message?.Invoke() ?? $"Expected {expected} within {precision} of {actual}.");
		}
	}

	public static void AreEqual(float expected, float actual, int precision, Func<string> message = null)
	{
		var delta = (float) Math.Pow(10, -precision);
		if (Math.Abs(expected - actual) > delta)
		{
			Fail(message?.Invoke() ?? $"Expected {expected} within {delta} of {actual}.");
		}
	}

	public static void AreNotEqual(double expected, double actual, int precision, Func<string> message = null)
	{
		var delta = Math.Pow(10, -precision);
		if (Math.Abs(expected - actual) <= delta)
		{
			Fail(message?.Invoke() ?? $"Expected {expected} not within {delta} of {actual}.");
		}
	}

	public static void Collection<T>(IEnumerable<T> collection, params Action<T>[] elementInspectors)
	{
		var list = collection.ToList();
		AreEqual(elementInspectors.Length, list.Count, () => "Collection length did not match inspector count.");
		for (var i = 0; i < elementInspectors.Length; i++)
		{
			elementInspectors[i](list[i]);
		}
	}

	public static void Contains(string actual, string expected, StringComparison comparison)
	{
		if ((actual == null) || (actual.IndexOf(expected, comparison) < 0))
		{
			Fail($"Expected string to contain '{expected}'.");
		}
	}

	public static void DoesNotContain(string actual, string expected)
	{
		if ((actual != null) && actual.Contains(expected))
		{
			Fail($"Expected string not to contain '{expected}'.");
		}
	}

	public static void DoesNotContain<T>(IEnumerable<T> collection, Func<T, bool> predicate)
	{
		DoesNotContain(predicate, collection);
	}

	public static void EndsWith(string actual, string expected)
	{
		if ((actual == null) || !actual.EndsWith(expected))
		{
			Fail($"Expected string to end with '{expected}'.");
		}
	}

	public static void Equivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual)
	{
		var actualList = actual.ToList();
		foreach (var item in expected)
		{
			if (!actualList.Contains(item))
			{
				Fail("Expected equivalent collection to contain " + item);
			}
		}
	}

	[DoesNotReturn]
	public static void Fail()
	{
		Fail("Assert.Fail");
	}

	public static void InRange<T>(T actual, T low, T high) where T : IComparable<T>
	{
		if ((actual.CompareTo(low) < 0) || (actual.CompareTo(high) > 0))
		{
			Fail($"Value {actual} was not in range [{low}, {high}].");
		}
	}

	public static T IsAssignableFrom<T>(object value)
	{
		if (value is T typed)
		{
			return typed;
		}

		Fail($"Expected {typeof(T).FullName} but got {value?.GetType().FullName ?? "null"}.");
		return default;
	}

	public static void IsAssignableFrom(Type expectedType, object value)
	{
		if ((expectedType == null) || (value == null) || !expectedType.IsInstanceOfType(value))
		{
			Fail($"Expected {expectedType?.FullName} but got {value?.GetType().FullName ?? "null"}.");
		}
	}

	public static void IsFalse(bool condition, string message)
	{
		IsFalse(condition, () => message);
	}

	public static void IsNotType<T>(object value)
	{
		if (value is T)
		{
			Fail($"Did not expect instance of {typeof(T).FullName}.");
		}
	}

	public static T IsNotNull<T>(T value) where T : class
	{
		if (value == null)
		{
			Fail("The condition was incorrectly null and should have been not null.");
		}

		return value;
	}

	public static T IsNotNull<T>(T? value) where T : struct
	{
		if (!value.HasValue)
		{
			Fail("The condition was incorrectly null and should have been not null.");
		}

		return value.Value;
	}

	public static void IsTrue(bool condition, string message)
	{
		IsTrue(condition, () => message);
	}

	public static T IsType<T>(object value)
	{
		return IsAssignableFrom<T>(value);
	}

	public static T IsType<T>(object value, bool exactMatch)
	{
		if (exactMatch)
		{
			AreEqual(typeof(T), value?.GetType());
			return (T) value;
		}

		return IsAssignableFrom<T>(value);
	}

	public static void IsType(Type expectedType, object value)
	{
		IsAssignableFrom(expectedType, value);
	}

	public static void NotEmpty(IEnumerable source)
	{
		if (!source.Cast<object>().Any())
		{
			Fail("Expected the collection to be not empty.");
		}
	}

	public static void NotSame(object expected, object actual)
	{
		if (ReferenceEquals(expected, actual))
		{
			Fail("Expected the instances not to be the same.");
		}
	}

	public static RaisedEvent<T> Raises<T>(Action<EventHandler<T>> attach, Action<EventHandler<T>> detach, Action testCode)
		where T : EventArgs
	{
		T args = null;
		object sender = null;

		void Handler(object s, T e)
		{
			sender = s;
			args = e;
		}

		attach(Handler);
		try
		{
			testCode();
		}
		finally
		{
			detach(Handler);
		}

		IsNotNull(args, () => "Event was not raised.");
		return new RaisedEvent<T>(sender, args);
	}

	public static void Same(object expected, object actual)
	{
		if (!ReferenceEquals(expected, actual))
		{
			Fail("Expected the instances to be the same.");
		}
	}

	public static void Single<T>(IEnumerable<T> collection, T expected)
	{
		AreEqual(1, collection.Count(x => Equals(x, expected)));
	}

	public static void StartsWith(string actual, string expected)
	{
		if ((actual == null) || !actual.StartsWith(expected))
		{
			Fail($"Expected string to start with '{expected}'.");
		}
	}

	public static Exception Throws(Type exceptionType, Action testCode)
	{
		try
		{
			testCode();
		}
		catch (Exception ex) when (exceptionType.IsInstanceOfType(ex))
		{
			return ex;
		}

		Fail("Expected exception of type " + exceptionType.FullName + ".");
		return null;
	}

	public static T Throws<T>(Action testCode) where T : Exception
	{
		T caught = null;
		ExpectedException<T>(testCode, ex => caught = ex);
		return caught;
	}

	public static T Throws<T>(Func<object> testCode) where T : Exception
	{
		return Throws<T>(() => { testCode(); });
	}

	public static T Throws<T>(string paramName, Action testCode) where T : ArgumentException
	{
		var ex = Throws<T>(testCode);
		AreEqual(paramName, ex.ParamName);
		return ex;
	}

	public static T Throws<T>(string paramName, Func<object> testCode) where T : ArgumentException
	{
		return Throws<T>(paramName, () => { testCode(); });
	}

	public static async Task<T> ThrowsAsync<T>(Func<Task> testCode) where T : Exception
	{
		try
		{
			await testCode();
		}
		catch (T ex)
		{
			return ex;
		}
		catch (Exception ex)
		{
			Fail($"The expected exception was not thrown. {ex.GetType()} thrown instead.");
		}

		Fail("Expected exception of type " + typeof(T).FullName + ".");
		return null;
	}

	public static T ThrowsAny<T>(Func<object> testCode) where T : Exception
	{
		return Throws<T>(testCode);
	}

	#endregion
}
