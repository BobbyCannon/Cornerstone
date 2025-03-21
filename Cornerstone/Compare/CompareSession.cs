#region References

using System;
using System.Collections.Generic;
using Cornerstone.Exceptions;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// The session to manage the comparison of two values.
/// </summary>
/// <typeparam name="T"> The data type of the actual value. </typeparam>
/// <typeparam name="T2"> The data type of the expected value. </typeparam>
public class CompareSession<T, T2> : CompareSession
{
	#region Constructors

	/// <summary>
	/// Initialize a new compare session.
	/// </summary>
	/// <param name="expected"> The value that is expected. </param>
	/// <param name="actual"> The value to compare with expected. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	public CompareSession(T expected, T2 actual, ComparerSettings settings) : base(settings)
	{
		Actual = actual;
		Expected = expected;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The actual value to compare to Expected.
	/// </summary>
	public T2 Actual { get; }

	/// <summary>
	/// The expected value.
	/// </summary>
	public T Expected { get; }

	#endregion

	#region Methods

	public bool AreEqual()
	{
		return Result == CompareResult.AreEqual;
	}

	/// <summary>
	/// Assert if the <see cref="CompareSession.Result" /> does not match the expected result.
	/// </summary>
	/// <param name="expectedResult"> The expected result. </param>
	/// <param name="prefix"> An optional prefix for the difference message. </param>
	public void Assert(CompareResult expectedResult, string prefix = null)
	{
		Assert(expectedResult, () => prefix);
	}

	/// <summary>
	/// Assert if the <see cref="CompareSession.Result" /> does not match the expected result.
	/// </summary>
	/// <param name="expectedResult"> The expected result. </param>
	/// <param name="prefix"> An optional prefix for the difference message. </param>
	public void Assert(CompareResult expectedResult, Func<string> prefix = null)
	{
		if (Result == expectedResult)
		{
			return;
		}

		if (prefix != null)
		{
			Differences.Insert(0, Environment.NewLine);
			Differences.Insert(0, Environment.NewLine);
			Differences.Insert(0, prefix.Invoke());
		}
		
		if (Differences.Length == 0)
		{
			Differences.AppendLine($"Expected [{expectedResult}] Result but got [{Result}] with values {Expected}.");
		}

		Differences.Insert(0, Environment.NewLine);

		throw new CompareException(Differences.ToString());
	}

	/// <summary>
	/// Run the compare for this session.
	/// </summary>
	public void Compare()
	{
		InternalProcess(this, Expected, Actual);
	}

	#endregion
}

/// <summary>
/// The session to manage the comparison of two values.
/// </summary>
public class CompareSession : ReferenceTracker
{
	#region Constructors

	/// <summary>
	/// Initialize a new compare session.
	/// </summary>
	public CompareSession() : this(new ComparerSettings())
	{
	}

	/// <summary>
	/// Initialize a new compare session.
	/// </summary>
	/// <param name="settings"> The settings for the compare session. </param>
	public CompareSession(ComparerSettings settings)
	{
		Differences = new TextBuilder();
		Settings = settings;
		Path = new Stack<string>();
		Result = CompareResult.Inconclusive;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Track the difference message.
	/// </summary>
	public TextBuilder Differences { get; }

	/// <summary>
	/// The current path being compared.
	/// </summary>
	public Stack<string> Path { get; }

	/// <summary>
	/// The final results of the comparison.
	/// </summary>
	public CompareResult Result { get; private set; }

	/// <summary>
	/// The settings for the compare session.
	/// </summary>
	public ComparerSettings Settings { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Build an error message for the comparer
	/// </summary>
	/// <param name="message"> On optional message to include. </param>
	/// <returns> The error message. </returns>
	public void AddDifference(string message)
	{
		if (Path.Count > 0)
		{
			InternalAppendDifferenceLine(string.Join(".", Path));
		}

		InternalAppendDifference(message);
	}

	/// <summary>
	/// Build an error message for the comparer
	/// </summary>
	/// <param name="expected"> The expected value in string format. </param>
	/// <param name="actual"> The actual value in string format. </param>
	/// <param name="shouldEqual"> True if the values should have been equal otherwise false. </param>
	/// <param name="message"> On optional message to include. </param>
	/// <returns> The error message. </returns>
	public void AddDifference(object expected, object actual, bool shouldEqual, Func<string> message = null)
	{
		UpdateResult(CompareResult.NotEqual);

		if (Path.Count > 0)
		{
			InternalAppendDifferenceLine(string.Join(".", Path));
		}

		var actualMessage = message?.Invoke();

		if (!string.IsNullOrWhiteSpace(actualMessage))
		{
			InternalAppendDifference(actualMessage);
			InternalAppendDifference(string.Empty);
		}

		if ((Differences.Length > 0)
			&& !Differences.EndsWithNewLine())
		{
			InternalAppendDifference(" ");
		}

		if (shouldEqual)
		{
			InternalAppendDifference(expected?.ToString() ?? "null");
			InternalAppendDifference("\r\n **** != ****\r\n");
			InternalAppendDifference(actual?.ToString() ?? "null");
			InternalAppendDifference("\r\n");
			return;
		}

		InternalAppendDifference("Should not have equaled the value below");
		InternalAppendDifference(actual?.ToString() ?? "null");
		InternalAppendDifference("\r\n");
	}

	/// <summary>
	/// The internal compare
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <typeparam name="T2"> </typeparam>
	/// <param name="session"> </param>
	/// <param name="expected"> </param>
	/// <param name="actual"> </param>
	/// <param name="message"> </param>
	/// <returns> </returns>
	internal static void InternalProcess<T, T2>(CompareSession session, T expected, T2 actual, Func<string> message = null)
	{
		if (session.Path.Count > session.Settings.MaxDepth)
		{
			session.UpdateResult(CompareResult.AreEqual);
			return;
		}

		if (Equals(expected, default(T)) && Equals(actual, default(T2)))
		{
			session.UpdateResult(CompareResult.AreEqual);
			return;
		}

		// Both are not null and if one is then the items do not match
		if (Equals(expected, null) || Equals(actual, null))
		{
			session.AddDifference(expected, actual, true, message);
			return;
		}

		if (expected.Equals(actual))
		{
			session.UpdateResult(CompareResult.AreEqual);
			return;
		}

		foreach (var comparer in Comparer.Comparers)
		{
			if (!comparer.IsSupported(session, expected, actual))
			{
				// The comparer should use expected to find the comparer
				continue;
			}

			var result = comparer.Compare(session, expected, actual, message);
			session.UpdateResult(result);
			return;
		}

		if ((typeof(T) == typeof(T2))
			&& EqualityComparer<T>.Default.Equals(expected,
				actual is T tActual ? tActual : default
			))
		{
			session.UpdateResult(CompareResult.AreEqual);
			return;
		}

		session.InternalAppendDifference($"The type [{expected.GetType().FullName ?? "null"}] is not supported by the {nameof(Comparer)}.");
	}

	internal void UpdateResult(CompareResult result)
	{
		if ((Result == result) || (Result == CompareResult.NotEqual))
		{
			return;
		}

		Result = result;
	}

	/// <summary>
	/// Add the difference line.
	/// </summary>
	/// <param name="value"> The value to be appended. </param>
	private void InternalAppendDifference(string value)
	{
		Differences.Append(value);
	}

	/// <summary>
	/// Add the difference line.
	/// </summary>
	/// <param name="value"> The value to be appended. </param>
	private void InternalAppendDifferenceLine(string value)
	{
		Differences.AppendLine(value);
	}

	#endregion
}