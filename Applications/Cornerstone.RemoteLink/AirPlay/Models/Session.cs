#region References

using Cornerstone.RemoteLink.AirPlay.Listeners;
using Cornerstone.RemoteLink.AirPlay.Models.Enums;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Models;

public class Session
{
	#region Fields

	public MirroringListener MirroringListener = null;

	public bool? MirroringSession = null;

	#endregion

	#region Constructors

	public Session(string sessionId)
	{
		SessionId = sessionId;
	}

	#endregion

	#region Properties

	public byte[] AesIv { get; set; } = null;
	public byte[] AesKey { get; set; } = null;
	public int AudioCompressionType { get; set; } = -1; // ct: 0=PCM, 1=ALAC, 2=AAC, 8=AAC-ELD
	public AudioFormat AudioFormat { get; set; } = AudioFormat.Unknown;
	public int AudioSamplesPerFrame { get; set; } = 0; // spf: samples per frame (e.g. 352, 480)
	public bool AudioSessionReady => AudioFormat != AudioFormat.Unknown;

	public byte[] DecryptedAesKey { get; set; } = null;

	public byte[] EcdhOurs { get; set; } = null;
	public byte[] EcdhShared { get; set; } = null;
	public byte[] EcdhTheirs { get; set; } = null;
	public byte[] EdTheirs { get; set; } = null;
	public bool FairPlayReady => (KeyMsg != null) && (EcdhShared != null) && (AesKey != null) && (AesIv != null);
	public bool FairPlaySetupCompleted => (KeyMsg != null) && (EcdhShared != null) && (PairVerified ?? false);
	public int? Height { get; set; } = null;
	public int? HeightSource { get; set; } = null;

	public byte[] KeyMsg { get; set; } = null;
	public bool MirroringSessionReady => (StreamConnectionId != null) && MirroringSession.HasValue ? MirroringSession.Value : false;

	public bool PairCompleted => (EcdhShared != null) && (PairVerified ?? false);

	public bool? PairVerified { get; set; } = null;
	public long? Pts { get; set; } = null;

	public string SessionId { get; }

	public byte[] SpsPps { get; set; } = null;
	public bool SpsPpsPending { get; set; }
	public string StreamConnectionId { get; set; } = null;
	public int? Width { get; set; } = null;
	public int? WidthSource { get; set; } = null;

	#endregion
}