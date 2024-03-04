#region References

using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class ExceptionExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToDetailedString()
	{
		var ex = new CornerstoneException("Message");
		var actual = ex.ToDetailedString();
		AreEqual("Cornerstone.Exceptions.CornerstoneException\r\nMessage", actual);
	}

	#endregion
}