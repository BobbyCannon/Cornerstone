#region References

using System;
using System.IO;
using Cornerstone.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Security;

[TestClass]
public class BiometricChallengeFileTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EnsurePersistedReplacesAChangedFileWithTheSignedChallenge()
	{
		WithDirectory(directory =>
		{
			var path = Path.Combine(directory, "Biometric.challenge");
			File.WriteAllText(path, "saved-challenge");
			var file = Create(directory, "device-id");
			AreEqual("saved-challenge", file.GetChallengeValue());
			File.WriteAllText(path, "tampered");
			file.EnsurePersisted();
			AreEqual("saved-challenge", File.ReadAllText(path));
		});
	}

	[TestMethod]
	public void EnsurePersistedSkipsAMatchingFile()
	{
		WithDirectory(directory =>
		{
			var path = Path.Combine(directory, "Biometric.challenge");
			File.WriteAllText(path, "device-id");
			var file = Create(directory, "device-id");
			AreEqual("device-id", file.GetChallengeValue());
			var info = new FileInfo(path);
			info.IsReadOnly = true;
			try
			{
				file.EnsurePersisted();
				AreEqual("device-id", File.ReadAllText(path));
			}
			finally
			{
				info.IsReadOnly = false;
			}
		});
	}

	[TestMethod]
	public void EnsurePersistedWritesAMissingChallenge()
	{
		WithDirectory(directory =>
		{
			var file = Create(directory, "device-id");
			AreEqual("device-id", file.GetChallengeValue());
			file.EnsurePersisted();
			AreEqual("device-id", File.ReadAllText(Path.Combine(directory, "Biometric.challenge")));
		});
	}

	[TestMethod]
	public void DeleteRemovesTheSavedChallenge()
	{
		WithDirectory(directory =>
		{
			var deviceId = "device-id";
			var file = Create(directory, () => deviceId);
			AreEqual("device-id", file.GetChallengeValue());
			file.Remember();
			file.Delete();
			IsFalse(File.Exists(Path.Combine(directory, "Biometric.challenge")));
			deviceId = "next-id";
			AreEqual("next-id", file.GetChallengeValue());
		});
	}

	[TestMethod]
	public void EmptyFileDoesNotUseTheDeviceId()
	{
		WithDirectory(directory =>
		{
			var path = Path.Combine(directory, "Biometric.challenge");
			File.WriteAllText(path, string.Empty);
			var file = Create(directory, "device-id");
			IsNull(file.GetChallengeValue());
			File.WriteAllText(path, "saved-challenge");
			AreEqual("saved-challenge", file.GetChallengeValue());
		});
	}

	[TestMethod]
	public void MissingFileUsesTheDeviceId()
	{
		WithDirectory(directory =>
		{
			var deviceId = "device-id";
			var file = Create(directory, () => deviceId);
			AreEqual("device-id", file.GetChallengeValue());
			deviceId = "changed";
			AreEqual("device-id", file.GetChallengeValue());
		});
	}

	[TestMethod]
	public void PresetChallengeSkipsTheFileUntilRemember()
	{
		WithDirectory(directory =>
		{
			var path = Path.Combine(directory, "Biometric.challenge");
			File.WriteAllText(path, "from-disk");
			var file = new BiometricChallengeFile(() => directory, () => "device-id", "Biometric.challenge", "preset");
			AreEqual("preset", file.GetChallengeValue());
			file.Remember();
			AreEqual("preset", File.ReadAllText(path));
			IsFalse(Directory.GetFiles(directory, "*.tmp").Length > 0);
		});
	}

	[TestMethod]
	public void RememberWritesTheCachedChallenge()
	{
		WithDirectory(directory =>
		{
			var file = Create(directory, "device-id");
			AreEqual("device-id", file.GetChallengeValue());
			file.Remember();
			var path = Path.Combine(directory, "Biometric.challenge");
			AreEqual("device-id", File.ReadAllText(path));
			AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
		});
	}

	[TestMethod]
	public void UnreadableFileReturnsNull()
	{
		WithDirectory(directory =>
		{
			var path = Path.Combine(directory, "Biometric.challenge");
			File.WriteAllText(path, "saved-challenge");
			var file = Create(directory, "device-id");
			using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				IsNull(file.GetChallengeValue());
			}

			AreEqual("saved-challenge", file.GetChallengeValue());
		});
	}

	private static BiometricChallengeFile Create(string directory, string deviceId)
	{
		return Create(directory, () => deviceId);
	}

	private static BiometricChallengeFile Create(string directory, Func<string> deviceId)
	{
		return new BiometricChallengeFile(() => directory, deviceId, "Biometric.challenge", null);
	}

	private static void WithDirectory(Action<string> test)
	{
		var directory = Path.Combine(Path.GetTempPath(), "BiometricChallengeFileTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			test(directory);
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	#endregion
}
