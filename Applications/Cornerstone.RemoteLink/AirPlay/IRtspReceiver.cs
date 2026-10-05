#region References

using Cornerstone.RemoteLink.AirPlay.Models.Audio;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

public interface IRtspReceiver
{
	#region Methods

	void OnAudioFlush();
	void OnData(H264Data data);
	void OnMirroringHeader(int payloadType, int payloadSize);
	void OnMirroringStarted();
	void OnMirroringStopped();
	void OnPCMData(PcmData data);
	void OnSetVolume(decimal volume);

	#endregion
}