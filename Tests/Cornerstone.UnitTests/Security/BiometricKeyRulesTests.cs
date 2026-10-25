#region References

using System.Threading.Tasks;
using Cornerstone.Platforms.Windows;
using Cornerstone.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Security.Credentials;

#endregion

namespace Cornerstone.UnitTests.Security;

[TestClass]
public class BiometricKeyRulesTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AndroidPerUseKeyRejectsATimeWindowAndDeviceCredential()
	{
		const int biometricStrong = 2;
		const int deviceCredential = 1;
		IsTrue(BiometricKeyRules.AndroidKeyIsPerUse(true, true, 256, 0, biometricStrong, biometricStrong, deviceCredential));
		IsTrue(BiometricKeyRules.AndroidKeyIsPerUse(true, true, 256, -1, biometricStrong, biometricStrong, deviceCredential));
		IsFalse(BiometricKeyRules.AndroidKeyIsPerUse(true, true, 256, 30, biometricStrong, biometricStrong, deviceCredential));
		IsFalse(BiometricKeyRules.AndroidKeyIsPerUse(true, true, 256, 0, biometricStrong | deviceCredential, biometricStrong, deviceCredential));
		IsFalse(BiometricKeyRules.AndroidKeyIsPerUse(true, true, 256, 0, deviceCredential, biometricStrong, deviceCredential));
		IsFalse(BiometricKeyRules.AndroidKeyIsPerUse(false, true, 256, 0, biometricStrong, biometricStrong, deviceCredential));
		IsFalse(BiometricKeyRules.AndroidKeyIsPerUse(true, false, 256, 0, biometricStrong, biometricStrong, deviceCredential));
		IsFalse(BiometricKeyRules.AndroidKeyIsPerUse(true, true, 128, 0, biometricStrong, biometricStrong, deviceCredential));
	}

	[TestMethod]
	public void HasNotBeenSetupRequiresSupportAndAMissingKey()
	{
		IsFalse(BiometricKeyRules.HasBeenSetup(false, true));
		IsFalse(BiometricKeyRules.HasNotBeenSetup(false, false));
		IsTrue(BiometricKeyRules.HasBeenSetup(true, true));
		IsFalse(BiometricKeyRules.HasNotBeenSetup(true, true));
		IsFalse(BiometricKeyRules.HasBeenSetup(true, false));
		IsTrue(BiometricKeyRules.HasNotBeenSetup(true, false));
	}

	[TestMethod]
	public void IosBiometrySetChangeRequiresADifferentStoredState()
	{
		var stored = new byte[] { 1, 2, 3, 4 };
		IsFalse(BiometricKeyRules.IosBiometrySetChanged(null, stored));
		IsFalse(BiometricKeyRules.IosBiometrySetChanged(stored, null));
		IsFalse(BiometricKeyRules.IosBiometrySetChanged(stored, new byte[] { 1, 2, 3, 4 }));
		IsTrue(BiometricKeyRules.IosBiometrySetChanged(stored, new byte[] { 1, 2, 3, 5 }));
		IsTrue(BiometricKeyRules.IosBiometrySetChanged(stored, new byte[] { 1, 2 }));
	}

	[TestMethod]
	public void LockedOrUnknownOpenStillHasKeyMaterial()
	{
		IsTrue(BiometricKeyRules.KeyMaterialExists(WindowsBiometricKeyService.ToOpenStatus(KeyCredentialStatus.Success)));
		IsTrue(BiometricKeyRules.KeyMaterialExists(WindowsBiometricKeyService.ToOpenStatus(KeyCredentialStatus.SecurityDeviceLocked)));
		IsTrue(BiometricKeyRules.KeyMaterialExists(WindowsBiometricKeyService.ToOpenStatus(KeyCredentialStatus.UnknownError)));
		IsTrue(BiometricKeyRules.KeyMaterialExists(WindowsBiometricKeyService.ToOpenStatus(KeyCredentialStatus.UserCanceled)));
		IsFalse(BiometricKeyRules.KeyMaterialExists(WindowsBiometricKeyService.ToOpenStatus(KeyCredentialStatus.NotFound)));
	}

	[TestMethod]
	public async Task StubIsUnavailableAndReturnsNull()
	{
		var stub = new BiometricKeyServiceStub();
		IsFalse(stub.IsSupported);
		IsFalse(stub.HasBeenSetup);
		IsFalse(stub.HasNotBeenSetup);
		IsFalse(stub.StaleKeyRemoved);
		AreEqual(string.Empty, stub.DisplayName);
		IsNull(await stub.GetVaultKeyAsync());
	}

	#endregion
}
