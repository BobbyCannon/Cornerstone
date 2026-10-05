#region References

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.AirPlay.Listeners;
using Cornerstone.RemoteLink.AirPlay.Managers;
using Cornerstone.RemoteLink.AirPlay.Models.Audio;
using Cornerstone.RemoteLink.AirPlay.Models.Configs;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;
using Makaretu.Dns;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

public class AirPlayReceiver : IRtspReceiver, IAirPlayReceiver, IDisposable
{
	#region Constants

	public const string AirPlayType = "_airplay._tcp";
	public const string AirTunesType = "_raop._tcp";

	#endregion

	#region Fields

	private readonly ushort _airPlayPort;
	private readonly AirTunesListener _airTunesListener;
	private readonly ushort _airTunesPort;
	private readonly string _deviceId;
	private readonly string _instance;

	private MulticastService _mdns;

	#endregion

	#region Constructors

	public AirPlayReceiver(AirPlayReceiverConfig aprConfig)
	{
		if (aprConfig == null)
		{
			throw new ArgumentNullException(nameof(aprConfig));
		}

		_airTunesPort = aprConfig.AirTunesPort != 0 ? aprConfig.AirTunesPort : (ushort) 5000;
		_airPlayPort = aprConfig.AirPlayPort != 0 ? aprConfig.AirPlayPort : (ushort) 7000;
		_deviceId = string.IsNullOrWhiteSpace(aprConfig.DeviceMacAddress) ? "11:22:33:44:55:66" : aprConfig.DeviceMacAddress;
		_instance = aprConfig.Instance ?? throw new ArgumentNullException(nameof(aprConfig.Instance));
		_airTunesListener = new AirTunesListener(this, _airTunesPort, _airPlayPort);
	}

	#endregion

	#region Properties

	public int MirroringLastSize { get; private set; }
	public int MirroringLastType { get; private set; }

	public int MirroringType0 { get; private set; }
	public int MirroringType1 { get; private set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		StopAsync().GetAwaiter().GetResult();
	}

	public void OnAudioFlush()
	{
		OnAudioFlushReceived?.Invoke(this, EventArgs.Empty);
	}

	public void OnData(H264Data data)
	{
		OnH264DataReceived?.Invoke(this, data);
	}

	public void OnMirroringHeader(int payloadType, int payloadSize)
	{
		MirroringLastType = payloadType;
		MirroringLastSize = payloadSize;
		if (payloadType == 0)
		{
			MirroringType0++;
		}
		else if (payloadType == 1)
		{
			MirroringType1++;
		}

		OnMirroringProgressReceived?.Invoke(this, EventArgs.Empty);
	}

	public void OnMirroringStarted()
	{
		MirroringType0 = 0;
		MirroringType1 = 0;
		MirroringLastType = 0;
		MirroringLastSize = 0;
		OnMirroringStartedReceived?.Invoke(this, EventArgs.Empty);
	}

	public void OnMirroringStopped()
	{
		OnMirroringStoppedReceived?.Invoke(this, EventArgs.Empty);
	}

	public void OnPCMData(PcmData data)
	{
		OnPCMDataReceived?.Invoke(this, data);
	}

	public void OnSetVolume(decimal volume)
	{
		OnSetVolumeReceived?.Invoke(this, volume);
	}

	public async Task StartListeners(CancellationToken cancellationToken)
	{
		await _airTunesListener.StartAsync(cancellationToken).ConfigureAwait(false);
	}

	public Task StartMdnsAsync()
	{
		if (string.IsNullOrWhiteSpace(_deviceId))
		{
			throw new ArgumentNullException(_deviceId);
		}

		var rDeviceId = new Regex("^(([0-9a-fA-F][0-9a-fA-F]):){5}([0-9a-fA-F][0-9a-fA-F])$");
		var mDeviceId = rDeviceId.Match(_deviceId);
		if (!mDeviceId.Success)
		{
			throw new ArgumentException("Device id must be a mac address", _deviceId);
		}

		var deviceIdInstance = string.Join(string.Empty, mDeviceId.Groups[2].Captures) + mDeviceId.Groups[3].Value;

		_mdns = new MulticastService();
		var sd = new ServiceDiscovery(_mdns);

		foreach (var ip in MulticastService.GetIPAddresses())
		{
			Console.WriteLine($"IP address {ip}");
		}

		_mdns.NetworkInterfaceDiscovered += (s, e) =>
		{
			foreach (var nic in e.NetworkInterfaces)
			{
				Console.WriteLine($"NIC '{nic.Name}'");
			}
		};

		// Internally 'ServiceProfile' create the SRV record
		var airTunes = new ServiceProfile($"{deviceIdInstance}@{_instance}", AirTunesType, _airTunesPort);
		airTunes.AddProperty("ch", "2");
		airTunes.AddProperty("cn", "0,1,2,3");
		airTunes.AddProperty("et", "0,3,5");
		airTunes.AddProperty("md", "0,1,2");
		airTunes.AddProperty("sr", "44100");
		airTunes.AddProperty("ss", "16");
		airTunes.AddProperty("da", "true");
		airTunes.AddProperty("sv", "false");
		airTunes.AddProperty("ft", "0x5A7FFFF7,0x1E"); // 0x4A7FFFF7, 0xE
		airTunes.AddProperty("am", "AppleTV5,3");
		airTunes.AddProperty("pk", "29fbb183a58b466e05b9ab667b3c429d18a6b785637333d3f0f3a34baa89f45e");
		airTunes.AddProperty("sf", "0x4");
		airTunes.AddProperty("tp", "UDP");
		airTunes.AddProperty("vn", "65537");
		airTunes.AddProperty("vs", "220.68");
		airTunes.AddProperty("vv", "2");

		/*
		* ch	2	audio channels: stereo
		* cn	0,1,2,3	audio codecs
		* et	0,3,5	supported encryption types
		* md	0,1,2	supported metadata types
		* pw	false	does the speaker require a password?
		* sr	44100	audio sample rate: 44100 Hz
		* ss	16	audio sample size: 16-bit
		*/

		// Internally 'ServiceProfile' create the SRV record
		var airPlay = new ServiceProfile(_instance, AirPlayType, _airPlayPort);
		airPlay.AddProperty("deviceid", _deviceId);
		airPlay.AddProperty("features", "0x5A7FFFF7,0x1E"); // 0x4A7FFFF7
		airPlay.AddProperty("flags", "0x4");
		airPlay.AddProperty("model", "AppleTV5,3");
		airPlay.AddProperty("pk", "29fbb183a58b466e05b9ab667b3c429d18a6b785637333d3f0f3a34baa89f45e");
		airPlay.AddProperty("pi", "aa072a95-0318-4ec3-b042-4992495877d3");
		airPlay.AddProperty("srcvers", "220.68");
		airPlay.AddProperty("vv", "2");

		sd.Advertise(airTunes);
		sd.Advertise(airPlay);

		_mdns.Start();

		return Task.CompletedTask;
	}

	public async Task DisconnectMirroringAsync()
	{
		_airTunesListener?.DisconnectClients();
		await SessionManager.Current.StopMirroringSessionsAsync().ConfigureAwait(false);
		OnMirroringStopped();
	}

	public async Task StopAsync()
	{
		try
		{
			if (_airTunesListener != null)
			{
				await _airTunesListener.StopAsync().ConfigureAwait(false);
			}
		}
		catch
		{
		}

		_mdns?.Stop();
		_mdns = null;
	}

	#endregion

	#region Events

	public event EventHandler OnAudioFlushReceived;
	public event EventHandler<H264Data> OnH264DataReceived;
	public event EventHandler OnMirroringProgressReceived;
	public event EventHandler OnMirroringStartedReceived;
	public event EventHandler OnMirroringStoppedReceived;
	public event EventHandler<PcmData> OnPCMDataReceived;
	public event EventHandler<decimal> OnSetVolumeReceived;

	#endregion
}