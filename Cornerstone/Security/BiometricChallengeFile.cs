#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.Security;

/// <summary>
/// Challenge text signed by a platform key. Callers that already stored a signature depend on the saved text staying stable.
/// A missing file uses the device id. An empty or unreadable file returns null and does not fall through to the device id.
/// </summary>
internal sealed class BiometricChallengeFile
{
	#region Fields

	private string _challengeValue;
	private readonly Func<string> _deviceId;
	private readonly Func<string> _directory;
	private readonly string _fileName;

	#endregion

	#region Constructors

	public BiometricChallengeFile(Func<string> directory, Func<string> deviceId, string fileName, string challengeValue)
	{
		_directory = directory;
		_deviceId = deviceId;
		_fileName = fileName;
		_challengeValue = challengeValue;
	}

	#endregion

	#region Methods

	public void Delete()
	{
		var path = GetPath();
		if (string.IsNullOrEmpty(path))
		{
			_challengeValue = null;
			return;
		}

		File.Delete(path);
		_challengeValue = null;
	}

	/// <summary>
	/// Writes the challenge that was just signed when the file is missing or different.
	/// A file that already matches is left in place.
	/// </summary>
	public void EnsurePersisted()
	{
		var challenge = _challengeValue;
		var path = GetPath();
		if (string.IsNullOrEmpty(challenge) || string.IsNullOrEmpty(path))
		{
			return;
		}

		try
		{
			if (File.Exists(path) && (File.ReadAllText(path) == challenge))
			{
				return;
			}
		}
		catch (Exception)
		{
			// The file is present but unreadable. Replace it with the challenge that was signed.
		}

		Remember();
	}

	public string GetChallengeValue()
	{
		if (!string.IsNullOrEmpty(_challengeValue))
		{
			return _challengeValue;
		}

		var path = GetPath();
		if (!string.IsNullOrEmpty(path))
		{
			try
			{
				var saved = File.ReadAllText(path);
				if (string.IsNullOrEmpty(saved))
				{
					return null;
				}

				_challengeValue = saved;
				return _challengeValue;
			}
			catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
			{
				// No challenge file yet. The device id below is the challenge.
			}
			catch
			{
				// The file is present but unreadable. Signing the device id would not unwrap an existing vault.
				return null;
			}
		}

		_challengeValue = _deviceId?.Invoke();
		return string.IsNullOrEmpty(_challengeValue) ? null : _challengeValue;
	}

	public void Remember()
	{
		var challenge = _challengeValue;
		var path = GetPath();
		if (string.IsNullOrEmpty(challenge) || string.IsNullOrEmpty(path))
		{
			return;
		}

		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		// Rename into place so a crash cannot truncate the challenge a caller already signed.
		var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(temporaryPath, challenge);
			File.Move(temporaryPath, path, true);
		}
		catch
		{
			try
			{
				File.Delete(temporaryPath);
			}
			catch
			{
				// The failed save is the error the caller has to surface.
			}

			throw;
		}
	}

	private string GetPath()
	{
		var directory = _directory?.Invoke();
		if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(_fileName))
		{
			return null;
		}

		return Path.Combine(directory, _fileName);
	}

	#endregion
}