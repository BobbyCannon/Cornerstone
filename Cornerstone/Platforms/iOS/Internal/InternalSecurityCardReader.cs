#region References

using System;
using System.Linq;
using CoreFoundation;
using CoreNFC;
using Foundation;

#endregion

namespace Cornerstone.Platforms.iOS.Internal;

internal class InternalSecurityCardReader : NFCTagReaderSessionDelegate
{
	#region Fields

	private INFCTag _tag;

	#endregion

	#region Constructors

	/// <summary>
	/// Default constructor
	/// </summary>
	public InternalSecurityCardReader()
	{
	}

	#endregion

	#region Properties

	public bool IsAvailable => NFCReaderSession.ReadingAvailable;

	public bool IsListening { get; private set; }

	private NFCTagReaderSession NfcSession { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Event raised when NFC tags are detected
	/// </summary>
	/// <param name="session"> iOS <see cref="NFCTagReaderSession" /> </param>
	/// <param name="tags"> Array of iOS <see cref="INFCTag" /> </param>
	public override void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags)
	{
		if (!tags.Any())
		{
			return;
		}

		_tag = tags.First();
	}

	/// <summary>
	/// Event raised when an error happened during detection
	/// </summary>
	/// <param name="session"> iOS <see cref="NFCTagReaderSession" /> </param>
	/// <param name="error"> iOS <see cref="NSError" /> </param>
	public override void DidInvalidate(NFCTagReaderSession session, NSError error)
	{
		Console.WriteLine($"NFC Session Invalidated: Code={error.Code}, Domain={error.Domain}, Description={error.LocalizedDescription}");
		NfcSession = null;
	}

	public void StartListening()
	{
		if (!IsAvailable || IsListening)
		{
			return;
		}

		StartSession();
	}

	private void StartSession()
	{
		NfcSession = new NFCTagReaderSession(NFCPollingOption.Iso14443, this, DispatchQueue.MainQueue);
		NfcSession?.BeginSession();
		IsListening = true;
	}

	/// <summary>
	/// Stops tags detection
	/// </summary>
	public void StopListening()
	{
		NfcSession?.InvalidateSession();
		NfcSession = null;
		IsListening = false;
	}

	#endregion
}