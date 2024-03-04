#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Exceptions;
using Cornerstone.UnitTests.Compare;
using Cornerstone.UnitTests.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Convert;

[TestClass]
public class ConverterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConvertTo()
	{
		AreEqual(null, Converter.ConvertTo<string>(null));

		ExpectedException<ArgumentNullException>(
			() => Converter.ConvertTo<DateTime>(null),
			"Value cannot be null. (Parameter 'from')"
		);

		ExpectedException<ConversionException>(
			() => DateTime.UtcNow.ConvertTo<SampleAccount>(),
			"Failed to convert [System.DateTime] to [Cornerstone.UnitTests.Resources.SampleAccount] type."
		);
	}

	[TestMethod]
	public void TryConvertTo()
	{
		AreEqual(true, Converter.TryConvertTo(null, typeof(string), out var value));
		AreEqual(null, value);

		AreEqual(false, Converter.TryConvertTo(null, typeof(DateTime), out value));
		AreEqual(false, DateTime.UtcNow.TryConvertTo(typeof(ComparerTests.Person), out value));
	}

	#endregion
}