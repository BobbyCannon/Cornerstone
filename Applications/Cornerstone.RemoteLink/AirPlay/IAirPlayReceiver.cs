#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.AirPlay.Models.Audio;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

public interface IAirPlayReceiver
{
	#region Properties

	int MirroringLastSize { get; }
	int MirroringLastType { get; }

	int MirroringType0 { get; }
	int MirroringType1 { get; }

	#endregion

	#region Methods

	Task StartListeners(CancellationToken cancellationToken);

	Task StartMdnsAsync();

	Task StopAsync();

	#endregion

	#region Events

	event EventHandler OnAudioFlushReceived;
	event EventHandler<H264Data> OnH264DataReceived;
	event EventHandler OnMirroringProgressReceived;
	event EventHandler OnMirroringStartedReceived;
	event EventHandler OnMirroringStoppedReceived;
	event EventHandler<PcmData> OnPCMDataReceived;
	event EventHandler<decimal> OnSetVolumeReceived;

	#endregion
}