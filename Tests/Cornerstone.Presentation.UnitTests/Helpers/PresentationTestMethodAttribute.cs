#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Headless;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[AttributeUsage(AttributeTargets.Method)]
public class PresentationTestMethodAttribute : TestMethodAttribute
{
	#region Properties

	public string Skip { get; set; }

	public int Timeout { get; set; }

	protected virtual bool DispatchToHeadlessSession => false;

	#endregion

	#region Methods

	public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
	{
		if (!string.IsNullOrEmpty(Skip))
		{
			return
			[
				new TestResult
				{
					Outcome = UnitTestOutcome.Ignored,
					LogOutput = Skip
				}
			];
		}

		var method = testMethod.MethodInfo;
		var befores = GetBeforeAfterAttributes(method).ToList();
		foreach (var before in befores)
		{
			before.Before(method);
		}

		try
		{
			if (DispatchToHeadlessSession)
			{
				var session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(TestApplication).Assembly);
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
				return await session.Dispatch(async () =>
				{
					var results = await base.ExecuteAsync(testMethod);
					try
					{
						Dispatcher.UIThread.RunJobs();
					}
					catch (Exception ex)
					{
						// The posted job runs after the test method returns. Keep that failure
						// on the result so MSTest does not report it as an attribute fault.
						foreach (var result in results)
						{
							if (result.Outcome == UnitTestOutcome.Passed)
							{
								result.Outcome = UnitTestOutcome.Failed;
								result.TestFailureException = ex;
							}
						}
					}

					return results;
				}, cts.Token);
			}

			return await base.ExecuteAsync(testMethod);
		}
		finally
		{
			for (var i = befores.Count - 1; i >= 0; i--)
			{
				befores[i].After(method);
			}
		}
	}

	internal static bool HasSkip(MethodInfo method)
	{
		if (method.GetCustomAttribute<IgnoreAttribute>(true) != null)
		{
			return true;
		}

		var test = method.GetCustomAttribute<PresentationTestMethodAttribute>(true);
		return (test != null) && !string.IsNullOrEmpty(test.Skip);
	}

	internal static IEnumerable<object[]> SkippedDataPlaceholder(MethodInfo method)
	{
		yield return new object[method.GetParameters().Length];
	}

	private static IEnumerable<BeforeAfterTestAttribute> GetBeforeAfterAttributes(MethodInfo method)
	{
		foreach (var attribute in method.GetCustomAttributes<BeforeAfterTestAttribute>(true))
		{
			yield return attribute;
		}

		if (method.DeclaringType != null)
		{
			foreach (var attribute in method.DeclaringType.GetCustomAttributes<BeforeAfterTestAttribute>(true))
			{
				yield return attribute;
			}

			foreach (var attribute in method.DeclaringType.Assembly.GetCustomAttributes<BeforeAfterTestAttribute>())
			{
				yield return attribute;
			}
		}
	}

	#endregion
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class HeadlessTestMethodAttribute : PresentationTestMethodAttribute
{
	#region Properties

	protected override bool DispatchToHeadlessSession => true;

	#endregion
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TestDataAttribute : Attribute, ITestDataSource
{
	#region Fields

	private readonly object[] _arguments;
	private readonly string _memberName;

	#endregion

	#region Constructors

	public TestDataAttribute(string memberName, params object[] arguments)
	{
		_memberName = memberName;
		_arguments = arguments;
	}

	#endregion

	#region Properties

	public Type MemberType { get; set; }

	#endregion

	#region Methods

	public IEnumerable<object[]> GetData(MethodInfo methodInfo)
	{
		if (PresentationTestMethodAttribute.HasSkip(methodInfo))
		{
			return PresentationTestMethodAttribute.SkippedDataPlaceholder(methodInfo);
		}

		var type = MemberType ?? methodInfo.DeclaringType;
		var member = (MemberInfo) type.GetProperty(_memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			?? type.GetField(_memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			?? (MemberInfo) type.GetMethod(_memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);

		object value;
		if (member is PropertyInfo property)
		{
			value = property.GetValue(null);
		}
		else if (member is FieldInfo field)
		{
			value = field.GetValue(null);
		}
		else if (member is MethodInfo method)
		{
			value = _arguments is { Length: > 0 } ? method.Invoke(null, _arguments) : method.Invoke(null, null);
		}
		else
		{
			throw new InvalidOperationException("TestData source '" + _memberName + "' was not found.");
		}

		if (value is IEnumerable<object[]> rows)
		{
			return rows;
		}

		if (value is IEnumerable enumerable)
		{
			return enumerable.Cast<object>().Select(x => x as object[] ?? [x]);
		}

		throw new InvalidOperationException("TestData source '" + _memberName + "' did not return rows.");
	}

	public string GetDisplayName(MethodInfo methodInfo, object[] data)
	{
		if (data == null)
		{
			return methodInfo.Name;
		}

		return methodInfo.Name + " (" + string.Join(", ", data.Select(x => System.Convert.ToString(x, CultureInfo.InvariantCulture))) + ")";
	}

	#endregion
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ClassDataSourceAttribute : Attribute, ITestDataSource
{
	#region Fields

	private readonly Type _classType;

	#endregion

	#region Constructors

	public ClassDataSourceAttribute(Type classType)
	{
		_classType = classType;
	}

	#endregion

	#region Methods

	public IEnumerable<object[]> GetData(MethodInfo methodInfo)
	{
		if (PresentationTestMethodAttribute.HasSkip(methodInfo))
		{
			return PresentationTestMethodAttribute.SkippedDataPlaceholder(methodInfo);
		}

		var instance = Activator.CreateInstance(_classType);
		return (IEnumerable<object[]>) instance;
	}

	public string GetDisplayName(MethodInfo methodInfo, object[] data)
	{
		if (data == null)
		{
			return methodInfo.Name;
		}

		return methodInfo.Name + " (" + string.Join(", ", data.Select(x => System.Convert.ToString(x, CultureInfo.InvariantCulture))) + ")";
	}

	#endregion
}

public interface ITestLog
{
	#region Methods

	void WriteLine(string message);

	#endregion
}

public sealed class NullTestLog : ITestLog
{
	#region Methods

	public void WriteLine(string message)
	{
	}

	#endregion
}

public static class Record
{
	#region Methods

	public static Exception Exception(Action testCode)
	{
		try
		{
			testCode();
			return null;
		}
		catch (Exception ex)
		{
			return ex;
		}
	}

	#endregion
}

public class ThrowsException : Exception
{
	#region Constructors

	public ThrowsException(string message)
		: base(message)
	{
	}

	#endregion

	#region Methods

	public static ThrowsException ForIncorrectExceptionType(Type expected, Exception actual)
	{
		return new ThrowsException("Expected " + expected + " but got " + actual.GetType());
	}

	public static ThrowsException ForNoException(Type expected)
	{
		return new ThrowsException("Expected " + expected + " but no exception was thrown.");
	}

	#endregion
}

public class EqualException : Exception
{
	#region Constructors

	public EqualException(object expected, object actual)
		: base("Expected: " + expected + " Actual: " + actual)
	{
	}

	#endregion

	#region Methods

	public static EqualException ForMismatchedValues(object expected, object actual)
	{
		return new EqualException(expected, actual);
	}

	#endregion
}