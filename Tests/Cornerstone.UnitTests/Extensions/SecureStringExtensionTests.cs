#region References

using System.Security;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class SecureStringExtensionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AppendSecureStringToAnEmptySecureString()
	{
		using var secureString1 = new SecureString();
		using var secureString2 = "123456".ToSecureString();

		secureString1.Append(secureString2);

		AreEqual(secureString1.ToUnsecureString(), secureString2.ToUnsecureString());
		IsTrue(secureString1.IsEqual(secureString2));
	}

	[TestMethod]
	public void ToSecureString()
	{
		var secureString = "123456".ToSecureString(true);
		IsTrue(secureString.IsReadOnly(), "Secure string should have been read only.");

		var unsecureString = secureString.ToUnsecureString();
		AreEqual("123456", unsecureString);
	}

	#endregion
}