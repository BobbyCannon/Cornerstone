#region References

using System;
using Cornerstone.Compare;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Testing;

public class CornerstoneTestTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void AreEqual()
	{
		AreEqual("1", "1");
		ExpectedException<CompareException>(
			() => AreEqual("1", "2"),
			"1\r\n **** != ****\r\n2\r\n"
		);
	}

	[Test]
	public void AreNotEqual()
	{
		AreNotEqual("1", "2");
		ExpectedException<CornerstoneException>(
			() => AreNotEqual("1", "1"),
			"Expected [NotEqual] Result but got [AreEqual].\r\n"
		);
	}

	[Test]
	public void ExpectedExceptionDoesNotContainExpected()
	{
		try
		{
			ExpectedException<CornerstoneException>(() => throw new CornerstoneException("Foo"), "Bar");
		}
		catch (Exception ex)
		{
			IsTrue(ex.Message.Contains("does not contain expected <\"Bar\">."));
		}
	}

	[Test]
	public void ExpectedExceptionOfDifferentException()
	{
		try
		{
			ExpectedException<ArgumentException>(() => throw new CornerstoneException("Foo"), "Bar");
		}
		catch (Exception ex)
		{
			AreEqual("The expected exception was not thrown. Cornerstone.CornerstoneException thrown instead.", ex.Message);
		}
	}

	[Test]
	public void ExpectedExceptionWasNotThrown()
	{
		try
		{
			ExpectedException<CornerstoneException>(() => { }, string.Empty);
		}
		catch (Exception ex)
		{
			IsTrue("The expected exception was not thrown." == ex.Message);
		}
	}

	[Test]
	public void FailShouldAlwaysThrow()
	{
		ExpectedException<CornerstoneException>(() => Fail("This is an error message."), "This is an error message.");
	}

	[Test]
	public void IsFalseShouldThrowIfTrue()
	{
		IsFalse(false);
		ExpectedException<CornerstoneException>(() => IsFalse(true), "The condition was incorrectly true and should have been false.");
	}

	[Test]
	public void IsNotNullShouldThrowIfNull()
	{
		IsNotNull(new object());
		ExpectedException<CornerstoneException>(() => IsNotNull(null!), "The condition was incorrectly null and should have been not null.");
	}

	[Test]
	public void IsNullShouldThrowIfNotNull()
	{
		IsNull(null!);
		ExpectedException<CornerstoneException>(() => IsNull(true), "The condition was incorrectly not null and should have been null.");
	}

	[Test]
	public void IsTrueShouldThrowIfFalse()
	{
		IsTrue(true);
		ExpectedException<CornerstoneException>(() => IsTrue(false), "The condition was incorrectly false and should have been true.");
	}

	#endregion
}