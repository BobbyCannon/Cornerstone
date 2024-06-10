#region References

using Cornerstone.Extensions;
using Cornerstone.PowerShell.Security;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.PowerShell.Security;

[TestFixture]
public class WindowsCredentialManagerTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void WriteReadCredential()
	{
		WindowsCredential credential = null;

		try
		{
			credential = WindowsCredentialManager.WriteCredential("CornerstoneUnitTest", "foo@bar.com", "Hello World".ToSecureString());
			var actual = WindowsCredentialManager.ReadCredential("CornerstoneUnitTest", WindowsCredentialType.Generic);
			AreEqual(credential.ApplicationName, actual.ApplicationName);
			AreEqual(credential.UserName, actual.UserName);
			AreEqual(credential.Password, actual.Password);
		}
		finally
		{
			WindowsCredentialManager.Delete(credential);
		}
	}

	#endregion
}