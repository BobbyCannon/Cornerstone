#region References

using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests;

public class CornerstoneExceptionTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Constructors()
	{
		var exception = new CornerstoneException();
		AreEqual("Exception of type 'Cornerstone.CornerstoneException' was thrown.", exception.Message);
	}

	#endregion
}