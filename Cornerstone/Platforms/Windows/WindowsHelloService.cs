#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// Windows Hello key-credential wrapper. Consent UI must run on the UI thread;
/// otherwise the system prompt can appear behind the app window.
/// </summary>
[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IWindowsHelloService))]
public class WindowsHelloService : IWindowsHelloService
{
	#region Constants

	private const string BrokerImageName = "CredentialUIBroker.exe";
	private const string BrokerWindowClass = "Credential Dialog Xaml Host";
	private const string CoreWindowClass = "Windows.UI.Core.CoreWindow";
	private const string EnrollmentImageName = "BioEnrollmentHost.exe";
	private const string ChallengeFileName = "WindowsHello.challenge";
	private const int MaxWindowParentDepth = 32;
	private const int ProcessQueryLimitedInformation = 0x1000;

	#endregion

	#region Fields

	private string _challengeValue;
	private readonly IDispatcher _dispatcher;
	private bool? _isSupported;
	private readonly string _keyName;
	private readonly IRuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsHelloService(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: this(runtimeInformation.ApplicationName, null)
	{
		_runtimeInformation = runtimeInformation;
		_dispatcher = dispatcher;
	}

	public WindowsHelloService(string keyName, string challengeValue)
	{
		_keyName = keyName;
		_challengeValue = challengeValue;
		_dispatcher = null;
		_runtimeInformation = null;
	}

	#endregion

	#region Properties

	public bool HasBeenSetup => IsSupported && (GetCredentialStatus() == KeyCredentialStatus.Success);

	public bool HasNotBeenSetup => IsSupported && (GetCredentialStatus() != KeyCredentialStatus.Success);

	public bool IsSupported => _isSupported ??= WaitWithoutContext(() => KeyCredentialManager.IsSupportedAsync().AsTask());

	#endregion

	#region Methods

	public void DeleteVaultKeyAsync()
	{
		WaitWithoutContext(() => KeyCredentialManager.DeleteAsync(_keyName).AsTask());
		DeleteSavedChallenge();
		_challengeValue = null;
	}

	public async Task<byte[]> GetVaultKeyAsync()
	{
		var keyCredentialAvailable = await KeyCredentialManager.IsSupportedAsync();
		if (!keyCredentialAvailable)
		{
			return null;
		}

		var openKeyResult = await KeyCredentialManager.OpenAsync(_keyName);
		if (openKeyResult?.Status == KeyCredentialStatus.NotFound)
		{
			openKeyResult = await RunOnUiThread(() => RequestCreateWithForegroundAsync(_keyName));
			if (openKeyResult?.Status == KeyCredentialStatus.CredentialAlreadyExists)
			{
				openKeyResult = await KeyCredentialManager.OpenAsync(_keyName);
			}
		}

		if ((openKeyResult == null) || (openKeyResult.Status != KeyCredentialStatus.Success) || (openKeyResult.Credential == null))
		{
			return null;
		}

		var challenge = GetChallengeValue();
		if (string.IsNullOrEmpty(challenge))
		{
			return null;
		}

		var userKey = openKeyResult.Credential;
		var buffer = CryptographicBuffer.ConvertStringToBinary(challenge, BinaryStringEncoding.Utf8);
		var signResult = await RunOnUiThread(() => RequestSignWithForegroundAsync(userKey, buffer));
		if ((signResult == null) || (signResult.Status != KeyCredentialStatus.Success) || (signResult.Result == null))
		{
			return null;
		}

		return signResult.Result.ToArray();
	}

	/// <inheritdoc />
	public void RememberChallenge()
	{
		var challenge = _challengeValue;
		var path = GetChallengePath();
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

	private static void FindConsentWindows(List<nint> brokers, List<nint> enrollments)
	{
		brokers.Clear();
		enrollments.Clear();
		NativeGeneral.EnumWindows((hWnd, _) =>
		{
			var kind = GetConsentWindowKind(hWnd);
			if (kind == ConsentWindowKind.Broker)
			{
				brokers.Add(hWnd);
			}
			else if (kind == ConsentWindowKind.Enrollment)
			{
				enrollments.Add(hWnd);
			}

			return true;
		}, 0);
	}

	/// <summary>
	/// EnumWindows is topmost first, so the first handle that was not already visible is the one to raise.
	/// </summary>
	private static nint FirstUnseenWindow(List<nint> visible, HashSet<nint> alreadyVisible)
	{
		foreach (var window in visible)
		{
			if (!alreadyVisible.Contains(window))
			{
				return window;
			}
		}

		return 0;
	}

	/// <summary>
	/// Saved challenge when the challenge file exists. The device id is used only when that file is absent.
	/// An empty or unreadable file returns null so a later read can try again.
	/// Callers that already stored a signature depend on the saved text staying stable.
	/// </summary>
	private string GetChallengeValue()
	{
		if (!string.IsNullOrEmpty(_challengeValue))
		{
			return _challengeValue;
		}

		var path = GetChallengePath();
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
			catch (IOException ex) when ((ex is FileNotFoundException) || (ex is DirectoryNotFoundException))
			{
				// No challenge file yet. The device id below is the challenge.
			}
			catch
			{
				// The file is present but unreadable. Signing the device id would not unwrap an existing vault.
				return null;
			}
		}

		_challengeValue = _runtimeInformation?.DeviceId;
		return string.IsNullOrEmpty(_challengeValue) ? null : _challengeValue;
	}

	private void DeleteSavedChallenge()
	{
		var path = GetChallengePath();
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		File.Delete(path);
	}

	private string GetChallengePath()
	{
		var directory = _runtimeInformation?.ApplicationDataLocation;
		if (string.IsNullOrEmpty(directory))
		{
			return null;
		}

		return Path.Combine(directory, ChallengeFileName);
	}

	private static ConsentWindowKind GetConsentWindowKind(nint window)
	{
		if (!NativeGeneral.IsWindowVisible(window))
		{
			return ConsentWindowKind.None;
		}

		var classNameBuffer = new StringBuilder(256);
		if (NativeGeneral.GetClassName(window, classNameBuffer, classNameBuffer.Capacity) == 0)
		{
			return ConsentWindowKind.None;
		}

		var className = classNameBuffer.ToString();
		var brokerClass = string.Equals(className, BrokerWindowClass, StringComparison.Ordinal);
		var coreWindow = string.Equals(className, CoreWindowClass, StringComparison.Ordinal);
		if (!brokerClass && !coreWindow)
		{
			return ConsentWindowKind.None;
		}

		NativeGeneral.GetWindowThreadProcessId(window, out var processId);
		var imagePath = GetProcessImagePath((int) processId);
		if (IsSystem32Image(imagePath, BrokerImageName))
		{
			return ConsentWindowKind.Broker;
		}

		if (coreWindow && (IsSystem32Image(imagePath, EnrollmentImageName) || IsSystemSettingsImage(imagePath)))
		{
			return ConsentWindowKind.Enrollment;
		}

		return ConsentWindowKind.None;
	}

	private KeyCredentialStatus GetCredentialStatus()
	{
		try
		{
			var result = WaitWithoutContext(() => KeyCredentialManager.OpenAsync(_keyName).AsTask());
			return result?.Status ?? KeyCredentialStatus.UnknownError;
		}
		catch (Exception)
		{
			return KeyCredentialStatus.UnknownError;
		}
	}

	private static string GetProcessImagePath(int processId)
	{
		if (processId <= 0)
		{
			return null;
		}

		var handle = NativeGeneral.OpenProcess(ProcessQueryLimitedInformation, false, processId);
		if (handle == 0)
		{
			return null;
		}

		try
		{
			var buffer = new StringBuilder(1024);
			var size = buffer.Capacity;
			if (!NativeGeneral.QueryFullProcessImageName(handle, 0, buffer, ref size))
			{
				return null;
			}

			return buffer.ToString();
		}
		finally
		{
			NativeGeneral.CloseHandle(handle);
		}
	}

	private static bool IsSameWindowOrDescendant(nint window, nint ancestor)
	{
		var current = window;
		// A parent/owner cycle would leave the raise task running, and a completed PIN would never return.
		for (var depth = 0; (depth < MaxWindowParentDepth) && (current != 0); depth++)
		{
			if (current == ancestor)
			{
				return true;
			}

			var parent = NativeGeneral.GetParent(current);
			if (parent == 0)
			{
				parent = NativeGeneral.GetWindow(current, NativeGeneral.GwOwner);
			}

			if ((parent == 0) || (parent == current))
			{
				return false;
			}

			current = parent;
		}

		return false;
	}

	private static bool IsSystem32Image(string imagePath, string fileName)
	{
		if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(fileName))
		{
			return false;
		}

		var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		var expected = Path.Combine(windowsDirectory, "System32", fileName);
		return PathsEqual(NormalizeImagePath(imagePath), expected);
	}

	private static bool IsSystemSettingsImage(string imagePath)
	{
		if (string.IsNullOrEmpty(imagePath))
		{
			return false;
		}

		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(NormalizeImagePath(imagePath));
		}
		catch
		{
			return false;
		}

		if (!string.Equals(Path.GetFileName(fullPath), "SystemSettings.exe", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		if (PathsEqual(fullPath, Path.Combine(windowsDirectory, "ImmersiveControlPanel", "SystemSettings.exe")))
		{
			return true;
		}

		var directory = Path.GetDirectoryName(fullPath);
		var folder = Path.GetFileName(directory);
		if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(folder))
		{
			return false;
		}

		var systemApps = Path.Combine(windowsDirectory, "SystemApps");
		var parent = Path.GetDirectoryName(directory);
		return PathsEqual(parent, systemApps)
			&& folder.StartsWith("Microsoft.Windows.ImmersiveControlPanel_", StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeImagePath(string imagePath)
	{
		const string extendedPrefix = @"\\?\";
		if (imagePath.StartsWith(extendedPrefix, StringComparison.Ordinal))
		{
			return imagePath[extendedPrefix.Length..];
		}

		return imagePath;
	}

	private static bool PathsEqual(string left, string right)
	{
		if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
		{
			return false;
		}

		try
		{
			return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Restacks the consent window above the app and leaves it topmost so another topmost window cannot cover the PIN dialog.
	/// </summary>
	private static void RaiseConsentWindow(nint window)
	{
		var flags = NativeGeneral.SetWindowPosFlags.NoMove | NativeGeneral.SetWindowPosFlags.NoSize;
		NativeGeneral.SetWindowPos(window, new(-2), 0, 0, 0, 0, flags);
		NativeGeneral.SetWindowPos(window, new(-1), 0, 0, 0, 0, flags);
		NativeGeneral.SetForegroundWindow(window);
	}

	/// <summary>
	/// Watches until the WinRT call cancels. A dialog that takes more than a couple of seconds to appear is still raised.
	/// Handles already visible at the start belong to another prompt and stay put.
	/// Each new broker is restacked once and foregrounded at most once more.
	/// </summary>
	private static async Task RaiseHelloPromptAsync(CancellationToken token)
	{
		var visibleBrokers = new List<nint>();
		var visibleEnrollments = new List<nint>();
		FindConsentWindows(visibleBrokers, visibleEnrollments);
		var existingBrokers = new HashSet<nint>(visibleBrokers);
		var existingEnrollments = new HashSet<nint>(visibleEnrollments);
		var raisedBrokers = new HashSet<nint>();
		var retriedBrokers = new HashSet<nint>();
		var raisedEnrollments = new HashSet<nint>();

		while (!token.IsCancellationRequested)
		{
			FindConsentWindows(visibleBrokers, visibleEnrollments);
			var broker = FirstUnseenWindow(visibleBrokers, existingBrokers);
			if (broker != 0)
			{
				if (!IsSameWindowOrDescendant(NativeGeneral.GetForegroundWindow(), broker))
				{
					if (!raisedBrokers.Contains(broker))
					{
						raisedBrokers.Add(broker);
						RaiseConsentWindow(broker);
					}
					else if (!retriedBrokers.Contains(broker))
					{
						retriedBrokers.Add(broker);
						NativeGeneral.SetForegroundWindow(broker);
					}
				}
			}
			else
			{
				var enrollment = FirstUnseenWindow(visibleEnrollments, existingEnrollments);
				if ((enrollment != 0) && !raisedEnrollments.Contains(enrollment))
				{
					raisedEnrollments.Add(enrollment);
					if (!IsSameWindowOrDescendant(NativeGeneral.GetForegroundWindow(), enrollment))
					{
						NativeGeneral.SetForegroundWindow(enrollment);
					}
				}
			}

			try
			{
				await Task.Delay(50, token);
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}
	}

	private static async Task<KeyCredentialRetrievalResult> RequestCreateWithForegroundAsync(string keyName)
	{
		var raisePrompt = StartRaisingHelloPrompt();
		try
		{
			return await KeyCredentialManager.RequestCreateAsync(keyName, KeyCredentialCreationOption.FailIfExists);
		}
		finally
		{
			await StopRaisingHelloPrompt(raisePrompt.Cancellation, raisePrompt.RaiseTask);
		}
	}

	private static async Task<KeyCredentialOperationResult> RequestSignWithForegroundAsync(KeyCredential userKey, IBuffer buffer)
	{
		var raisePrompt = StartRaisingHelloPrompt();
		try
		{
			return await userKey.RequestSignAsync(buffer);
		}
		finally
		{
			await StopRaisingHelloPrompt(raisePrompt.Cancellation, raisePrompt.RaiseTask);
		}
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

	private static (CancellationTokenSource Cancellation, Task RaiseTask) StartRaisingHelloPrompt()
	{
		var cancellation = new CancellationTokenSource();
		var raiseTask = RaiseHelloPromptAsync(cancellation.Token);
		return (cancellation, raiseTask);
	}

	private static async Task StopRaisingHelloPrompt(CancellationTokenSource cancellation, Task raiseTask)
	{
		cancellation.Cancel();
		try
		{
			await raiseTask;
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			cancellation.Dispose();
		}
	}

	private static void WaitWithoutContext(Func<Task> start)
	{
		var captured = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(null);
		Task task;
		try
		{
			task = start();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(captured);
		}

		task.GetAwaiter().GetResult();
	}

	private static T WaitWithoutContext<T>(Func<Task<T>> start)
	{
		var captured = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(null);
		Task<T> task;
		try
		{
			task = start();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(captured);
		}

		return task.GetAwaiter().GetResult();
	}

	#endregion

	#region Enumerations

	private enum ConsentWindowKind
	{
		None,
		Broker,
		Enrollment
	}

	#endregion
}