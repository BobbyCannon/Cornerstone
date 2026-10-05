#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class SpanStringTokenizerTests
{
	#region Methods

	[PresentationTestMethod]
	public void ReadDoubleReadsValues()
	{
		var target = new SpanStringTokenizer("12.3,45.6");

		CornerstoneTest.AreEqual(12.3, target.ReadDouble());
		CornerstoneTest.AreEqual(45.6, target.ReadDouble());
		AssertThrows<FormatException>(target, t => t.ReadDouble());
	}

	[PresentationTestMethod]
	public void ReadInt32ReadsValues()
	{
		var target = new SpanStringTokenizer("123,456");

		CornerstoneTest.AreEqual(123, target.ReadInt32());
		CornerstoneTest.AreEqual(456, target.ReadInt32());
		AssertThrows<FormatException>(target, t => t.ReadInt32());
	}

	[PresentationTestMethod]
	public void ReadSpanAndReadStringReadsSame()
	{
		var target1 = new SpanStringTokenizer("abc,def");
		var target2 = new SpanStringTokenizer("abc,def");

		CornerstoneTest.AreEqual(target1.ReadString(), target2.ReadSpan().ToString());
		CornerstoneTest.IsTrue(target1.ReadSpan().SequenceEqual(target2.ReadString()));
	}

	[PresentationTestMethod]
	public void TryReadDoubleDoesntThrow()
	{
		var target = new SpanStringTokenizer("abc");

		CornerstoneTest.IsFalse(target.TryReadDouble(out var value));
	}

	[PresentationTestMethod]
	public void TryReadDoubleReadsValues()
	{
		var target = new SpanStringTokenizer("12.3,45.6");

		CornerstoneTest.IsTrue(target.TryReadDouble(out var value));
		CornerstoneTest.AreEqual(12.3, value);
		CornerstoneTest.IsTrue(target.TryReadDouble(out value));
		CornerstoneTest.AreEqual(45.6, value);
		CornerstoneTest.IsFalse(target.TryReadDouble(out value));
	}

	[PresentationTestMethod]
	public void TryReadInt32DoesntThrow()
	{
		var target = new SpanStringTokenizer("abc");

		CornerstoneTest.IsFalse(target.TryReadInt32(out var value));
	}

	[PresentationTestMethod]
	public void TryReadInt32ReadsValues()
	{
		var target = new SpanStringTokenizer("123,456");

		CornerstoneTest.IsTrue(target.TryReadInt32(out var value));
		CornerstoneTest.AreEqual(123, value);
		CornerstoneTest.IsTrue(target.TryReadInt32(out value));
		CornerstoneTest.AreEqual(456, value);
		CornerstoneTest.IsFalse(target.TryReadInt32(out value));
	}

	private static TException AssertThrows<TException>(SpanStringTokenizer tokenizer, TokenizerAction action)
		where TException : Exception
	{
		try
		{
			action(tokenizer);
		}
		catch (Exception ex)
		{
			if (ex.GetType() == typeof(TException))
			{
				return (TException) ex;
			}

			throw ThrowsException.ForIncorrectExceptionType(typeof(TException), ex);
		}

		throw ThrowsException.ForNoException(typeof(TException));
	}

	#endregion

	#region Delegates

	// Explicit delegate because C# generics do not allow ref structs.
	private delegate void TokenizerAction(SpanStringTokenizer tokenizer);

	#endregion
}