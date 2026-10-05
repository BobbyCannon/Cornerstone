#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

internal static class AssertHelper
{
	#region Methods

	public static void Equal<T>(T expected, T actual)
	{
		CornerstoneTest.AreEqual(expected, actual);
	}

	public static void False(bool condition, string message = null)
	{
		CornerstoneTest.IsFalse(condition, message);
	}

	public static void NotNull([NotNull] object value)
	{
		CornerstoneTest.IsNotNull(value);
	}

	public static void NotSame(object expected, object actual)
	{
		CornerstoneTest.NotSame(expected, actual);
	}

	public static void Null(object value)
	{
		CornerstoneTest.IsNull(value);
	}

	public static void Same(object expected, object actual)
	{
		CornerstoneTest.Same(expected, actual);
	}

	public static void True(bool condition, string message = null)
	{
		CornerstoneTest.IsTrue(condition, message);
	}

	#endregion
}