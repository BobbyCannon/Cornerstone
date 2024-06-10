#region References

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

#endregion

namespace Cornerstone.PowerShell.Security;

/// <summary>
/// The manager for Windows credential.
/// </summary>
public static class WindowsCredentialManager
{
	#region Methods

	/// <summary>
	/// Delete a Windows credential.
	/// </summary>
	/// <param name="credential"> </param>
	public static void Delete(WindowsCredential credential)
	{
		CredDelete(credential.ApplicationName, credential.CredentialType, 0);
		//Trace.Assert(result, "Failed to delete the credential.");
	}

	/// <summary>
	/// Enumerate all Windows WebCredential.
	/// </summary>
	/// <param name="filter"> An optional filter. </param>
	/// <returns> The Windows credentials. </returns>
	public static IEnumerable<WindowsCredential> EnumerateCredentials(Func<WindowsCredential, bool> filter = null)
	{
		var pCredentials = IntPtr.Zero;
		var response = new List<WindowsCredential>();

		try
		{
			var result = CredEnumerate(null, 0, out var count, out pCredentials);
			if (result)
			{
				for (var i = 0; i < count; i++)
				{
					var credential = Marshal.ReadIntPtr(pCredentials, i * Marshal.SizeOf(typeof(IntPtr)));
					using (var handle = new CriticalCredentialHandle(credential, true))
					{
						var c = ConvertCredential(handle.GetCredential());
						if ((filter != null) && !filter(c))
						{
							continue;
						}

						response.Add(c);
					}
				}
			}
		}
		finally
		{
			CredFree(pCredentials);
		}

		return response;
	}

	/// <summary>
	/// Read a single Windows credential.
	/// </summary>
	/// <param name="applicationName"> The name of the application. </param>
	/// <param name="type"> The type of the credential. </param>
	/// <returns> The read credential otherwise null. </returns>
	public static WindowsCredential ReadCredential(string applicationName,
		WindowsCredentialType type = WindowsCredentialType.Generic)
	{
		var read = CredRead(applicationName, type, 0, out var nCredPtr);
		if (!read)
		{
			return null;
		}

		using var handle = new CriticalCredentialHandle(nCredPtr, false);
		return ConvertCredential(handle.GetCredential());
	}

	/// <summary>
	/// Write the provided credential.
	/// </summary>
	/// <param name="applicationName"> The credential application name to write. </param>
	/// <param name="userName"> The credential user name to write. </param>
	/// <param name="password"> The credential password to write. </param>
	/// <param name="type"> The type of the credential. </param>
	/// <returns> The created Windows credential. </returns>
	public static WindowsCredential WriteCredential(string applicationName, string userName, SecureString password, WindowsCredentialType type = WindowsCredentialType.Generic)
	{
		return WriteCredential(new WindowsCredential(type, applicationName, userName, password));
	}

	/// <summary>
	/// Write the provided credential.
	/// </summary>
	/// <param name="credential"> The credential to write. </param>
	/// <returns> The created Windows credential. </returns>
	public static WindowsCredential WriteCredential(WindowsCredential credential)
	{
		if (credential.Password.Length > 256)
		{
			throw new ArgumentOutOfRangeException(nameof(credential), "The password has exceeded 256 bytes.");
		}

		var blob = Marshal.SecureStringToBSTR(credential.SecurePassword);
		var nativeCredential = new NativeCredential
		{
			AttributeCount = 0,
			Attributes = IntPtr.Zero,
			Comment = credential.Comment == null ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(credential.Comment),
			TargetAlias = IntPtr.Zero,
			Type = WindowsCredentialType.Generic,
			Persist = (uint) WindowsCredentialPersistence.LocalMachine,
			CredentialBlobSize = (uint) credential.Password.Length * 2,
			TargetName = Marshal.StringToCoTaskMemUni(credential.ApplicationName),
			CredentialBlob = blob,
			UserName = Marshal.StringToCoTaskMemUni(credential.UserName ?? Environment.UserName)
		};

		var written = CredWrite(ref nativeCredential, 0);
		var lastError = Marshal.GetLastWin32Error();
		var response = ConvertCredential(nativeCredential);

		Marshal.FreeCoTaskMem(nativeCredential.Comment);
		Marshal.FreeCoTaskMem(nativeCredential.TargetName);
		Marshal.ZeroFreeBSTR(nativeCredential.CredentialBlob);
		Marshal.FreeCoTaskMem(nativeCredential.UserName);

		if (!written)
		{
			throw new Exception($"CredWrite failed with the error code {lastError}.");
		}

		return response;
	}

	private static WindowsCredential ConvertCredential(NativeCredential nativeCredential)
	{
		var applicationName = Marshal.PtrToStringUni(nativeCredential.TargetName);
		var userName = Marshal.PtrToStringUni(nativeCredential.UserName);
		var comment = Marshal.PtrToStringUni(nativeCredential.Comment);
		var secret = new SecureString();

		if (nativeCredential.CredentialBlob != IntPtr.Zero)
		{
			for (var i = 0; i < nativeCredential.CredentialBlobSize; i += 2)
			{
				secret.AppendChar((char) Marshal.ReadInt16(nativeCredential.CredentialBlob, i));
			}
		}

		var credential = new WindowsCredential(nativeCredential.Type, applicationName, userName, secret, comment);
		return credential;
	}

	[DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
	private static extern bool CredDelete(string target, WindowsCredentialType type, int flags);

	[DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
	private static extern bool CredEnumerate(string filter, int flags, out int count, out IntPtr pCredentials);

	[DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
	private static extern void CredFree([In] IntPtr cred);

	[DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredRead(string target, WindowsCredentialType type, int reservedFlag, out IntPtr credentialPtr);

	[DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredWrite([In] ref NativeCredential userNativeCredential, [In] uint flags);

	#endregion

	#region Classes

	private sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
	{
		#region Fields

		private readonly bool _isPartOfEnumeration;

		#endregion

		#region Constructors

		public CriticalCredentialHandle(IntPtr preexistingHandle, bool isPartOfEnumeration)
		{
			_isPartOfEnumeration = isPartOfEnumeration;
			SetHandle(preexistingHandle);
		}

		#endregion

		#region Methods

		public NativeCredential GetCredential()
		{
			if (IsInvalid)
			{
				throw new InvalidOperationException("Invalid Critical Handle!");
			}

			return (NativeCredential) Marshal.PtrToStructure(handle, typeof(NativeCredential));
		}

		protected override bool ReleaseHandle()
		{
			if (IsInvalid)
			{
				return false;
			}

			if (!_isPartOfEnumeration)
			{
				// Only free credentials when doing single read.
				CredFree(handle);
			}

			SetHandleAsInvalid();
			return true;
		}

		#endregion
	}

	#endregion

	#region Structures

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NativeCredential
	{
		public readonly uint Flags;
		public WindowsCredentialType Type;
		public IntPtr TargetName;
		public IntPtr Comment;
		public readonly FILETIME LastWritten;
		public uint CredentialBlobSize;
		public IntPtr CredentialBlob;
		public uint Persist;
		public uint AttributeCount;
		public IntPtr Attributes;
		public IntPtr TargetAlias;
		public IntPtr UserName;
	}

	#endregion
}