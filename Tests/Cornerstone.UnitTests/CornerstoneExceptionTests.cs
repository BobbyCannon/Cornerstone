#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
public class CornerstoneExceptionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Constructors()
	{
		var exception = new CornerstoneException();
		AreEqual("Exception of type 'Cornerstone.CornerstoneException' was thrown.", exception.Message);
	}

	#endregion
}