#region References

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Hardware.Biometrics;
using Android.OS;
using Android.Runtime;
using Android.Security.Keystore;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

#endregion

[assembly: UsesPermission(Manifest.Permission.UseBiometric)]

namespace Cornerstone.Platforms.Android;

/// <summary>
/// Android Keystore AES key gated by a strong biometric. The vault secret is a random value wrapped by that key.
/// Each use authorizes that cipher through BiometricPrompt. An RSA signature is not used, because Android
/// Keystore signatures are not stable enough to unwrap the vault.
/// </summary>
[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IBiometricKeyService))]
public class AndroidBiometricKeyService : IBiometricKeyService
{
	#region Constants

	private const int BiometricStrongAuthenticator = (int) KeyPropertiesAuthType.BiometricStrong;
	private const int DeviceCredentialAuthenticator = (int) KeyPropertiesAuthType.DeviceCredential;
	private const int PerUseAuthenticationSeconds = 0;
	private const string CipherTransformation = "AES/GCM/NoPadding";
	private const int IvSize = 12;
	private const string KeyStoreName = "AndroidKeyStore";
	private const string SecretFileName = "BiometricKey.secret";
	private const int TagSizeBits = 128;

	#endregion

	#region Fields

	private readonly Func<string> _directory;
	private readonly IDispatcher _dispatcher;
	private readonly object _gate;
	private bool? _isSupported;
	private readonly string _keyName;
	private Task<byte[]> _pendingUnlock;
	private bool _staleKeyRemoved;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AndroidBiometricKeyService(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
	{
		var name = runtimeInformation.ApplicationName;
		_keyName = string.IsNullOrWhiteSpace(name) ? "BiometricVaultKey" : name;
		_dispatcher = dispatcher;
		_directory = () => runtimeInformation.ApplicationDataLocation;
		_gate = new object();
		_pendingUnlock = null;
		_staleKeyRemoved = false;
	}

	#endregion

	#region Properties

	public string DisplayName => IsSupported ? "Biometrics" : string.Empty;

	public bool HasBeenSetup
	{
		get
		{
			if (!IsSupported || !AliasExists() || !SecretFileExists())
			{
				return false;
			}

			// An unreadable key is kept. Only a key that is confirmed weaker than a per-use biometric key is absent.
			return InspectKey() != KeyVerdict.Unacceptable;
		}
	}

	public bool HasNotBeenSetup => IsSupported && !HasBeenSetup;

	public bool StaleKeyRemoved => _staleKeyRemoved;

	public bool IsSupported
	{
		get
		{
			if (_isSupported == true)
			{
				return true;
			}

			var supported = QuerySupported();
			if (supported)
			{
				_isSupported = true;
			}

			return supported;
		}
	}

	#endregion

	#region Methods

	public Task DeleteVaultKeyAsync()
	{
		return Task.Run(() =>
		{
			DeleteAlias();
			DeleteSecretFile();
		});
	}

	public Task<byte[]> GetVaultKeyAsync()
	{
		lock (_gate)
		{
			if ((_pendingUnlock != null) && !_pendingUnlock.IsCompleted)
			{
				return _pendingUnlock;
			}

			_pendingUnlock = RunOnUiThread(UnlockAsync);
			return _pendingUnlock;
		}
	}

	/// <inheritdoc />
	public void RememberChallenge()
	{
	}

	private bool AliasExists()
	{
		try
		{
			var keyStore = OpenKeyStore();
			return (keyStore != null) && keyStore.ContainsAlias(_keyName);
		}
		catch (Exception)
		{
			return false;
		}
	}

	private Cipher CreateDecryptCipher(byte[] wrapped)
	{
		var iv = new byte[IvSize];
		Buffer.BlockCopy(wrapped, 0, iv, 0, IvSize);
		var cipher = Cipher.GetInstance(CipherTransformation);
		cipher.Init(Javax.Crypto.CipherMode.DecryptMode, LoadSecretKey(), new GCMParameterSpec(TagSizeBits, iv));
		return cipher;
	}

	private Cipher CreateEncryptCipher()
	{
		var cipher = Cipher.GetInstance(CipherTransformation);
		cipher.Init(Javax.Crypto.CipherMode.EncryptMode, LoadSecretKey());
		return cipher;
	}

	private Task<byte[]> PromptAsync(Activity activity, Cipher cipher, Func<Cipher, byte[]> finish)
	{
		var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var executor = activity.MainExecutor;
		var callback = new CryptoAuthenticationCallback(completion, finish, Named("is locked. Try again later."));
		var prompt = new BiometricPrompt.Builder(activity)
			.SetTitle(string.IsNullOrEmpty(DisplayName) ? "Biometrics" : DisplayName)
			.SetSubtitle("Unlock the vault")
			.SetNegativeButton("Cancel", executor, new CancelListener(completion))
			.SetAllowedAuthenticators((int) BiometricManagerAuthenticators.BiometricStrong)
			.Build();
		// The cipher is authorized inside this prompt. A boolean prompt would leave the key usable
		// after any recent biometric, including the lock screen.
		prompt.Authenticate(new BiometricPrompt.CryptoObject(cipher), new CancellationSignal(), executor, callback);
		return completion.Task;
	}

	private static async Task CompleteOnUi<T>(Func<Task<T>> work, TaskCompletionSource<T> completion)
	{
		try
		{
			completion.SetResult(await work());
		}
		catch (Exception ex)
		{
			completion.SetException(ex);
		}
	}

	private static byte[] CopyCiphertext(byte[] wrapped)
	{
		var encrypted = new byte[wrapped.Length - IvSize];
		Buffer.BlockCopy(wrapped, IvSize, encrypted, 0, encrypted.Length);
		return encrypted;
	}

	private byte[] FinishEncrypt(Cipher cipher, byte[] secret)
	{
		var iv = cipher.GetIV();
		var encrypted = cipher.DoFinal(secret);
		if ((iv == null) || (iv.Length != IvSize) || (encrypted == null) || (encrypted.Length == 0))
		{
			throw new InvalidOperationException(Named("could not wrap its secret."));
		}

		var wrapped = new byte[iv.Length + encrypted.Length];
		Buffer.BlockCopy(iv, 0, wrapped, 0, iv.Length);
		Buffer.BlockCopy(encrypted, 0, wrapped, iv.Length, encrypted.Length);
		return wrapped;
	}

	private void DeleteAlias()
	{
		var keyStore = OpenKeyStore();
		if ((keyStore != null) && keyStore.ContainsAlias(_keyName))
		{
			keyStore.DeleteEntry(_keyName);
		}
	}

	private void DeleteSecretFile()
	{
		var path = GetSecretPath();
		if (!string.IsNullOrEmpty(path) && File.Exists(path))
		{
			File.Delete(path);
		}
	}

	private KeyVerdict InspectKey()
	{
		try
		{
			var keyStore = OpenKeyStore();
			if ((keyStore == null) || !keyStore.ContainsAlias(_keyName))
			{
				return KeyVerdict.Missing;
			}

			// A certificate entry is an older RSA key and cannot wrap the vault secret.
			if (keyStore.GetCertificate(_keyName) != null)
			{
				return KeyVerdict.Unacceptable;
			}

			var entry = keyStore.GetEntry(_keyName, null) as KeyStore.SecretKeyEntry;
			var secret = entry?.SecretKey;
			if (secret == null)
			{
				var key = keyStore.GetKey(_keyName, null);
				secret = key as ISecretKey;
				if ((secret == null) && (key != null))
				{
					return KeyVerdict.Unacceptable;
				}
			}

			if (secret == null)
			{
				return KeyVerdict.Unknown;
			}

			var factory = SecretKeyFactory.GetInstance(KeyProperties.KeyAlgorithmAes, KeyStoreName);
			var spec = factory.GetKeySpec(secret, Java.Lang.Class.FromType(typeof(KeyInfo)));
			var info = spec as KeyInfo;
			if (info == null)
			{
				info = spec?.JavaCast<KeyInfo>();
			}

			if (info == null)
			{
				return KeyVerdict.Unknown;
			}

			var acceptable = BiometricKeyRules.AndroidKeyIsPerUse(
				info.IsUserAuthenticationRequired,
				info.IsInvalidatedByBiometricEnrollment,
				info.KeySize,
				info.UserAuthenticationValidityDurationSeconds,
				info.UserAuthenticationType,
				BiometricStrongAuthenticator,
				DeviceCredentialAuthenticator);
			return acceptable ? KeyVerdict.Acceptable : KeyVerdict.Unacceptable;
		}
		catch (Exception)
		{
			// A failed read must not look like a weak key. The caller keeps the alias.
			return KeyVerdict.Unknown;
		}
	}

	private static bool IsPermanentlyInvalidated(Exception exception)
	{
		for (var current = exception; current != null; current = current.InnerException)
		{
			if (current is KeyPermanentlyInvalidatedException)
			{
				return true;
			}
		}

		return false;
	}

	private void EnsureKey()
	{
		var keyStore = OpenKeyStore();
		if (keyStore == null)
		{
			throw new InvalidOperationException("Android keystore is not available.");
		}

		// This alias must be the AES vault key. A certificate entry cannot wrap the secret.
		if (keyStore.ContainsAlias(_keyName) && (keyStore.GetCertificate(_keyName) != null))
		{
			keyStore.DeleteEntry(_keyName);
		}

		if (keyStore.ContainsAlias(_keyName))
		{
			var verdict = InspectKey();
			if (verdict == KeyVerdict.Acceptable)
			{
				return;
			}

			if (verdict == KeyVerdict.Unknown)
			{
				throw new InvalidOperationException(Named("key could not be verified."));
			}

			DeleteAlias();
		}

		var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KeyStoreName);
		var spec = new KeyGenParameterSpec.Builder(_keyName, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
			.SetBlockModes(KeyProperties.BlockModeGcm)
			.SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
			.SetKeySize(256)
			.SetUserAuthenticationRequired(true)
			.SetUnlockedDeviceRequired(true)
			.SetInvalidatedByBiometricEnrollment(true)
			.SetUserAuthenticationParameters(PerUseAuthenticationSeconds, BiometricStrongAuthenticator)
			.Build();
		generator.Init(spec);
		generator.GenerateKey();
		if (InspectKey() == KeyVerdict.Unacceptable)
		{
			DeleteAlias();
			throw new InvalidOperationException(Named("could not create a per-use biometric key."));
		}
	}

	private string GetSecretPath()
	{
		var directory = _directory?.Invoke();
		if (string.IsNullOrEmpty(directory))
		{
			return null;
		}

		return Path.Combine(directory, SecretFileName);
	}

	private IKey LoadSecretKey()
	{
		var keyStore = OpenKeyStore();
		var entry = keyStore?.GetEntry(_keyName, null) as KeyStore.SecretKeyEntry;
		if (entry?.SecretKey != null)
		{
			return entry.SecretKey;
		}

		var key = keyStore?.GetKey(_keyName, null);
		if (key == null)
		{
			throw new InvalidOperationException(Named("key is missing."));
		}

		return key;
	}

	private string Named(string action)
	{
		var name = DisplayName;
		if (string.IsNullOrEmpty(name))
		{
			name = "Biometrics";
		}

		return name + " " + action;
	}

	private KeyStore OpenKeyStore()
	{
		var keyStore = KeyStore.GetInstance(KeyStoreName);
		keyStore.Load(null, null);
		return keyStore;
	}

	private bool QuerySupported()
	{
		try
		{
			var context = AndroidPlatform.Activity ?? Application.Context;
			if (context == null)
			{
				return false;
			}

			var manager = context.GetSystemService(Context.BiometricService) as BiometricManager;
			if (manager == null)
			{
				return false;
			}

			var status = manager.CanAuthenticate((int) BiometricManagerAuthenticators.BiometricStrong);
			return (status == BiometricCode.Success) || (status == BiometricCode.ErrorNoneEnrolled);
		}
		catch (Exception)
		{
			return false;
		}
	}

	private byte[] ReadWrappedSecret()
	{
		var path = GetSecretPath();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return null;
		}

		var wrapped = File.ReadAllBytes(path);
		if ((wrapped == null) || (wrapped.Length <= IvSize))
		{
			return null;
		}

		return wrapped;
	}

	private async Task<T> RunOnUiThread<T>(Func<Task<T>> work)
	{
		if ((_dispatcher == null) || _dispatcher.CheckAccess())
		{
			return await work();
		}

		var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		_dispatcher.Post(() => { _ = CompleteOnUi(work, completion); }, DispatcherPriority.Send);
		return await completion.Task;
	}

	private bool SecretFileExists()
	{
		var path = GetSecretPath();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return false;
		}

		try
		{
			return new FileInfo(path).Length > IvSize;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private async Task<byte[]> UnlockAsync()
	{
		_staleKeyRemoved = false;
		if (!IsSupported)
		{
			throw new InvalidOperationException(Named("is not available on this device."));
		}

		var activity = AndroidPlatform.Activity;
		if (activity == null)
		{
			throw new InvalidOperationException(Named("needs the current Android activity."));
		}

		var wrapped = ReadWrappedSecret();
		if (wrapped != null)
		{
			var verdict = InspectKey();
			if (!AliasExists() || (verdict == KeyVerdict.Unacceptable))
			{
				// A weaker key cannot unwrap the saved secret. Setup calls again and creates a replacement.
				DeleteAlias();
				DeleteSecretFile();
				_staleKeyRemoved = true;
				return null;
			}

			try
			{
				var ciphertext = CopyCiphertext(wrapped);
				return await PromptAsync(activity, CreateDecryptCipher(wrapped), cipher => cipher.DoFinal(ciphertext));
			}
			catch (Exception ex) when (IsPermanentlyInvalidated(ex))
			{
				DeleteAlias();
				DeleteSecretFile();
				_staleKeyRemoved = true;
				return null;
			}
		}

		if (AliasExists())
		{
			var verdict = InspectKey();
			if (verdict == KeyVerdict.Unknown)
			{
				throw new InvalidOperationException(Named("key could not be verified."));
			}

			// No durable secret was stored for this alias. Setup calls again and creates one.
			DeleteAlias();
			_staleKeyRemoved = true;
			return null;
		}

		EnsureKey();
		var secret = RandomNumberGenerator.GetBytes(32);
		try
		{
			var wrappedSecret = await PromptAsync(activity, CreateEncryptCipher(), cipher => FinishEncrypt(cipher, secret));
			if (wrappedSecret == null)
			{
				DeleteAlias();
				CryptographicOperations.ZeroMemory(secret);
				return null;
			}

			WriteWrappedSecret(wrappedSecret);
			return secret;
		}
		catch (Exception)
		{
			DeleteAlias();
			DeleteSecretFile();
			CryptographicOperations.ZeroMemory(secret);
			throw;
		}
	}

	private void WriteWrappedSecret(byte[] wrapped)
	{
		var path = GetSecretPath();
		if (string.IsNullOrEmpty(path))
		{
			throw new InvalidOperationException(Named("could not store its secret."));
		}

		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllBytes(temporaryPath, wrapped);
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
			}

			throw;
		}
	}

	#endregion

	#region Enumerations

	private enum KeyVerdict
	{
		Missing,
		Acceptable,
		Unacceptable,
		Unknown
	}

	#endregion

	#region Classes

	private sealed class CancelListener : Java.Lang.Object, IDialogInterfaceOnClickListener
	{
		#region Fields

		private readonly TaskCompletionSource<byte[]> _completion;

		#endregion

		#region Constructors

		public CancelListener(TaskCompletionSource<byte[]> completion)
		{
			_completion = completion;
		}

		#endregion

		#region Methods

		public void OnClick(IDialogInterface dialog, int which)
		{
			_completion.TrySetResult(null);
		}

		#endregion
	}

	private sealed class CryptoAuthenticationCallback : BiometricPrompt.AuthenticationCallback
	{
		#region Fields

		private readonly TaskCompletionSource<byte[]> _completion;
		private readonly Func<Cipher, byte[]> _finish;
		private readonly string _lockedMessage;

		#endregion

		#region Constructors

		public CryptoAuthenticationCallback(TaskCompletionSource<byte[]> completion, Func<Cipher, byte[]> finish, string lockedMessage)
		{
			_completion = completion;
			_finish = finish;
			_lockedMessage = lockedMessage;
		}

		#endregion

		#region Methods

		public override void OnAuthenticationError(BiometricErrorCode errorCode, Java.Lang.ICharSequence errString)
		{
			if ((errorCode == BiometricErrorCode.Lockout) || (errorCode == BiometricErrorCode.LockoutPermanent))
			{
				var message = errString == null ? null : errString.ToString();
				_completion.TrySetException(new InvalidOperationException(
					string.IsNullOrEmpty(message) ? _lockedMessage : message));
				return;
			}

			_completion.TrySetResult(null);
		}

		public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
		{
			try
			{
				var cipher = result?.CryptoObject?.Cipher;
				if (cipher == null)
				{
					_completion.TrySetException(new InvalidOperationException("Biometrics did not authorize the vault key."));
					return;
				}

				var finished = _finish(cipher);
				if ((finished == null) || (finished.Length == 0))
				{
					_completion.TrySetException(new InvalidOperationException("Biometrics could not open the vault key."));
					return;
				}

				_completion.TrySetResult(finished);
			}
			catch (Exception ex)
			{
				_completion.TrySetException(ex);
			}
		}

		#endregion
	}

	#endregion
}