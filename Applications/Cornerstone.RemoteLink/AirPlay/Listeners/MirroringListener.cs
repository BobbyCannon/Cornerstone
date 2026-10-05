#region References

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.AirPlay.Crypto;
using Cornerstone.RemoteLink.AirPlay.Listeners.Bases;
using Cornerstone.RemoteLink.AirPlay.Managers;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;
using Cornerstone.RemoteLink.AirPlay.Utils;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Listeners;

public class MirroringListener : BaseTcpListener
{
	#region Constants

	public const string AIR_PLAY_STREAM_IV = "AirPlayStreamIV";
	public const string AIR_PLAY_STREAM_KEY = "AirPlayStreamKey";

	#endregion

	#region Fields

	private readonly IBufferedCipher _aesCtrDecrypt;
	private int _nextDecryptCount;

	private readonly byte[] _og = new byte[16];
	private readonly OriginalHax _originalHax = new();

	private readonly IRtspReceiver _receiver;
	private readonly string _sessionId;

	#endregion

	#region Constructors

	public MirroringListener(IRtspReceiver receiver, string sessionId, ushort port) : base(port, true)
	{
		_receiver = receiver;
		_sessionId = sessionId;

		_aesCtrDecrypt = CipherUtilities.GetCipher("AES/CTR/NoPadding");
	}

	#endregion

	#region Methods

	public override async Task OnRawReceivedAsync(TcpClient client, NetworkStream stream, CancellationToken cancellationToken)
	{
		// Get session by active-remove header value
		var session = await SessionManager.Current.GetSessionAsync(_sessionId);

		// If we have not decripted session AesKey
		if (session.DecryptedAesKey == null)
		{
			var decryptedAesKey = new byte[16];
			_originalHax.DecryptAesKey(session.KeyMsg, session.AesKey, decryptedAesKey);
			session.DecryptedAesKey = decryptedAesKey;
		}

		// Reset cipher state for new connection
		_nextDecryptCount = 0;
		Array.Clear(_og, 0, _og.Length);

		InitAesCtrCipher(session.DecryptedAesKey, session.EcdhShared, session.StreamConnectionId);

		var headerBuffer = new byte[128];

		try
		{
			do
			{
				// Read the first 4 bytes to determine packet type
				var readStart = 0;
				int ret;
				do
				{
					ret = await stream.ReadAsync(headerBuffer, readStart, 4 - readStart, cancellationToken);
					if (ret <= 0)
					{
						goto exit_loop;
					}
					readStart += ret;
				} while (readStart < 4);

				// Read remaining 124 bytes of the 128-byte header
				do
				{
					ret = await stream.ReadAsync(headerBuffer, readStart, 128 - readStart, cancellationToken);
					if (ret <= 0)
					{
						goto exit_loop;
					}
					readStart += ret;
				} while (readStart < 128);

				var header = new MirroringHeader(headerBuffer);
				if (!header.IsValid)
				{
					// Wrong size/type here means the TCP stream is already desynced.
					goto exit_loop;
				}

				_receiver.OnMirroringHeader(header.PayloadType, header.PayloadSize);

				if ((header.Width > 0) && (header.Height > 0))
				{
					session.Width = header.Width;
					session.Height = header.Height;
				}

				if (header.PayloadType == 1)
				{
					if (header.WidthSource > 0)
					{
						session.WidthSource = header.WidthSource;
					}
					if (header.HeightSource > 0)
					{
						session.HeightSource = header.HeightSource;
					}
				}

				try
				{
					byte[] payload = null;
					if (header.PayloadSize > 0)
					{
						payload = new byte[header.PayloadSize];

						readStart = 0;
						do
						{
							ret = await stream.ReadAsync(payload, readStart, header.PayloadSize - readStart, cancellationToken);
							if (ret <= 0)
							{
								goto exit_loop;
							}
							readStart += ret;
						} while (readStart < header.PayloadSize);
					}

					if ((header.PayloadType == 0) && (payload != null))
					{
						DecryptVideoData(payload, out var output);
						var framePts = header.PayloadPts;
						var width = session.Width ?? session.WidthSource ?? 1920;
						var height = session.Height ?? session.HeightSource ?? 1080;
						var prependSps = session.SpsPpsPending;
						session.SpsPpsPending = false;
						ProcessVideo(output, session.SpsPps, prependSps, framePts, width, height);
					}
					else if ((header.PayloadType == 1) && (payload != null))
					{
						ProcessSpsPps(payload, out var spsPps);
						session.SpsPps = spsPps;
						session.SpsPpsPending = spsPps != null;
						if ((spsPps != null) && (spsPps.Length > 0))
						{
							var codec = new H264Data();
							codec.FrameType = 7;
							codec.Data = spsPps;
							codec.Length = spsPps.Length;
							codec.Width = session.Width ?? session.WidthSource ?? 1920;
							codec.Height = session.Height ?? session.HeightSource ?? 1080;
							_receiver.OnData(codec);
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"Mirroring error: {e}");
				}

				if (header.PayloadType == 1)
				{
					await SessionManager.Current.CreateOrUpdateSessionAsync(_sessionId, session);
				}
			} while (client.Connected && stream.CanRead && !cancellationToken.IsCancellationRequested);
		}
		catch (OperationCanceledException)
		{
			// Normal shutdown - cancellation was requested
		}

		exit_loop:
		Console.WriteLine("Closing mirroring connection..");
	}

	private void DecryptVideoData(byte[] videoData, out byte[] output)
	{
		if (_nextDecryptCount > 0)
		{
			for (var i = 0; i < _nextDecryptCount; i++)
			{
				videoData[i] = (byte) (videoData[i] ^ _og[(16 - _nextDecryptCount) + i]);
			}
		}

		var encryptlen = ((videoData.Length - _nextDecryptCount) / 16) * 16;
		_aesCtrDecrypt.ProcessBytes(videoData, _nextDecryptCount, encryptlen, videoData, _nextDecryptCount);

		var restlen = (videoData.Length - _nextDecryptCount) % 16;
		var reststart = videoData.Length - restlen;
		_nextDecryptCount = 0;
		if (restlen > 0)
		{
			Array.Fill(_og, (byte) 0);
			Array.Copy(videoData, reststart, _og, 0, restlen);
			_aesCtrDecrypt.ProcessBytes(_og, 0, 16, _og, 0);
			Array.Copy(_og, 0, videoData, reststart, restlen);
			_nextDecryptCount = 16 - restlen;
		}

		output = videoData;
	}

	private void InitAesCtrCipher(byte[] aesKey, byte[] ecdhShared, string streamConnectionId)
	{
		var eaesKey = Utilities.Hash(aesKey, ecdhShared);

		var skey = Encoding.UTF8.GetBytes($"{AIR_PLAY_STREAM_KEY}{streamConnectionId}");
		var hash1 = Utilities.Hash(skey, Utilities.CopyOfRange(eaesKey, 0, 16));

		var siv = Encoding.UTF8.GetBytes($"{AIR_PLAY_STREAM_IV}{streamConnectionId}");
		var hash2 = Utilities.Hash(siv, Utilities.CopyOfRange(eaesKey, 0, 16));

		var decryptAesKey = new byte[16];
		var decryptAesIV = new byte[16];
		Array.Copy(hash1, 0, decryptAesKey, 0, 16);
		Array.Copy(hash2, 0, decryptAesIV, 0, 16);

		var keyParameter = ParameterUtilities.CreateKeyParameter("AES", decryptAesKey);
		var cipherParameters = new ParametersWithIV(keyParameter, decryptAesIV, 0, decryptAesIV.Length);

		_aesCtrDecrypt.Init(false, cipherParameters);
	}

	private void ProcessSpsPps(byte[] payload, out byte[] spsPps)
	{
		spsPps = null;
		if ((payload == null) || (payload.Length < 12))
		{
			return;
		}

		var h264 = new H264Codec();

		h264.Version = payload[0];
		h264.ProfileHigh = payload[1];
		h264.Compatibility = payload[2];
		h264.Level = payload[3];
		h264.Reserved6AndNal = payload[4];
		h264.Reserved3AndSps = payload[5];
		h264.LengthOfSps = (short) (((payload[6] & 255) << 8) + (payload[7] & 255));
		if ((h264.LengthOfSps <= 0) || ((8 + h264.LengthOfSps + 3) >= payload.Length))
		{
			return;
		}

		var sequence = new byte[h264.LengthOfSps];
		Array.Copy(payload, 8, sequence, 0, h264.LengthOfSps);
		h264.SequenceParameterSet = sequence;
		h264.NumberOfPps = payload[h264.LengthOfSps + 8];
		h264.LengthOfPps = (short) (((payload[h264.LengthOfSps + 9] & 0xFF) << 8) + (payload[h264.LengthOfSps + 10] & 0xFF));
		if ((h264.LengthOfPps <= 0) || ((h264.LengthOfSps + 11 + h264.LengthOfPps) > payload.Length))
		{
			return;
		}

		var picture = new byte[h264.LengthOfPps];
		Array.Copy(payload, h264.LengthOfSps + 11, picture, 0, h264.LengthOfPps);
		h264.PictureParameterSet = picture;

		if ((h264.LengthOfSps + h264.LengthOfPps) < 102400)
		{
			var spsPpsLen = h264.LengthOfSps + h264.LengthOfPps + 8;
			spsPps = new byte[spsPpsLen];

			spsPps[0] = 0;
			spsPps[1] = 0;
			spsPps[2] = 0;
			spsPps[3] = 1;

			Array.Copy(h264.SequenceParameterSet, 0, spsPps, 4, h264.LengthOfSps);

			spsPps[h264.LengthOfSps + 4] = 0;
			spsPps[h264.LengthOfSps + 5] = 0;
			spsPps[h264.LengthOfSps + 6] = 0;
			spsPps[h264.LengthOfSps + 7] = 1;

			Array.Copy(h264.PictureParameterSet, 0, spsPps, h264.LengthOfSps + 8, h264.LengthOfPps);
		}
		else
		{
			spsPps = null;
		}
	}

	private void ProcessVideo(byte[] payload, byte[] spsPps, bool prependSps, long pts, int widthSource, int heightSource)
	{
		if ((payload == null) || (payload.Length < 5))
		{
			return;
		}

		// Mirror video is AVCC (4-byte big-endian length, then NAL). Do not treat
		// 00 00 01 xx as Annex-B: that is a 256-511 byte NAL length.
		var offset = 0;
		while ((offset + 4) <= payload.Length)
		{
			var nc_len = ((payload[offset] & 0xFF) << 24) | ((payload[offset + 1] & 0xFF) << 16) |
				((payload[offset + 2] & 0xFF) << 8) | (payload[offset + 3] & 0xFF);

			if ((nc_len <= 0) || ((offset + 4 + nc_len) > payload.Length))
			{
				break;
			}

			payload[offset] = 0;
			payload[offset + 1] = 0;
			payload[offset + 2] = 0;
			payload[offset + 3] = 1;

			offset += 4 + nc_len;
		}

		var h264Data = new H264Data();
		h264Data.FrameType = payload[4] & 0x1f;

		// Prepend SPS/PPS to the first video packet after codec config, and to IDR/SEI.
		var nalType = h264Data.FrameType;
		var shouldPrepend = (spsPps != null) && (spsPps.Length > 0)
			&& (prependSps || (nalType == 5) || (nalType == 6));
		if (shouldPrepend)
		{
			var payloadOut = new byte[payload.Length + spsPps.Length];
			Array.Copy(spsPps, 0, payloadOut, 0, spsPps.Length);
			Array.Copy(payload, 0, payloadOut, spsPps.Length, payload.Length);

			h264Data.Data = payloadOut;
			h264Data.Length = payloadOut.Length;
		}
		else
		{
			h264Data.Data = payload;
			h264Data.Length = payload.Length;
		}

		h264Data.Pts = pts;
		h264Data.Width = widthSource;
		h264Data.Height = heightSource;

		_receiver.OnData(h264Data);
	}

	#endregion
}