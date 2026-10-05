#region References

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.AirPlay.Models.Enums;
using Cornerstone.RemoteLink.AirPlay.Utils;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Models.TcpListeners;

public class Request
{
	#region Constants

	public const string AIRTUNES_SERVER_VERSION = "AirTunes/220.68";

	#endregion

	#region Fields

	private readonly string _hex;

	#endregion

	#region Constructors

	public Request(string hex)
	{
		_hex = hex ?? throw new ArgumentNullException(nameof(hex));
		Headers = new HeadersCollection();

		Initialize();
	}

	#endregion

	#region Properties

	public byte[] Body { get; private set; }

	public HeadersCollection Headers { get; }

	public bool IsValid { get; private set; } = true;

	public string Path { get; private set; }

	public ProtocolType Protocol { get; private set; }

	public RequestType Type { get; private set; }

	#endregion

	#region Methods

	public Response GetBaseResponse()
	{
		var response = new Response(Protocol, StatusCode.OK);
		response.Headers.Add("Server", AIRTUNES_SERVER_VERSION);
		if (Headers.ContainsKey("CSeq"))
		{
			response.Headers.Add("CSeq", Headers["CSeq"]);
		}

		return response;
	}

	public Task<byte[]> GetFullRawAsync()
	{
		return Task.FromResult(_hex.HexToBytes());
	}

	private void Initialize()
	{
		// Split hex by '\r\n' (0D0A)
		var rows = _hex.Split("0D0A", StringSplitOptions.None);

		if (rows?.Any() == false)
		{
			IsValid = false;
			return;
		}

		var type = ResolveRequestType(rows[0]);
		if (!type.HasValue)
		{
			IsValid = false;
			return;
		}

		var path = ResolvePath(rows[0]);
		if (string.IsNullOrWhiteSpace(path))
		{
			IsValid = false;
			return;
		}

		var protocol = ResolveProtocol(rows[0]);
		if (!protocol.HasValue)
		{
			IsValid = false;
			return;
		}

		Type = type.Value;
		Path = path;
		Protocol = protocol.Value;

		foreach (var header in rows.Skip(1))
		{
			if (string.IsNullOrWhiteSpace(header))
			{
				// End of headers
				break;
			}

			var h = ResolveHeader(header);
			Headers.Add(h.Name, h);
		}

		if (Headers.ContainsKey("Content-Length"))
		{
			var contentLength = Headers.GetValue<int>("Content-Length");
			if (contentLength > 0)
			{
				// Some request can have body w/ '\r\n' chars
				// Use full hex request to extract body based on 'Content-Length'
				var requestBytes = _hex.HexToBytes();
				var bodyBytes = requestBytes.Skip(requestBytes.Length - contentLength).ToArray();

				if (contentLength == bodyBytes.Length)
				{
					Body = bodyBytes;
				}
				else
				{
					throw new Exception("wrong body length");
				}
			}
			else
			{
				Body = new byte[0];
			}
		}
	}

	private Header ResolveHeader(string hex)
	{
		return new Header(hex);
	}

	private string ResolvePath(string hex)
	{
		var r = new Regex("20(.*)20");
		var m = r.Match(hex);

		if (m.Success)
		{
			var pathHex = m.Groups[1].Value;
			var pathBytes = pathHex.HexToBytes();
			return Encoding.ASCII.GetString(pathBytes);
		}

		return null;
	}

	private ProtocolType? ResolveProtocol(string hex)
	{
		if (hex.EndsWith(ProtocolConst.HTTP10, StringComparison.OrdinalIgnoreCase))
		{
			return ProtocolType.HTTP10;
		}
		if (hex.EndsWith(ProtocolConst.HTTP11, StringComparison.OrdinalIgnoreCase))
		{
			return ProtocolType.HTTP11;
		}
		if (hex.EndsWith(ProtocolConst.RTSP10, StringComparison.OrdinalIgnoreCase))
		{
			return ProtocolType.RTSP10;
		}

		return null;
	}

	private RequestType? ResolveRequestType(string hex)
	{
		if (hex.StartsWith(RequestConst.GET, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.GET;
		}
		if (hex.StartsWith(RequestConst.POST, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.POST;
		}
		if (hex.StartsWith(RequestConst.SETUP, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.SETUP;
		}
		if (hex.StartsWith(RequestConst.RECORD, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.RECORD;
		}
		if (hex.StartsWith(RequestConst.GET_PARAMETER, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.GET_PARAMETER;
		}
		if (hex.StartsWith(RequestConst.SET_PARAMETER, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.SET_PARAMETER;
		}
		if (hex.StartsWith(RequestConst.OPTIONS, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.OPTIONS;
		}
		if (hex.StartsWith(RequestConst.ANNOUNCE, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.ANNOUNCE;
		}
		if (hex.StartsWith(RequestConst.FLUSH, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.FLUSH;
		}
		if (hex.StartsWith(RequestConst.TEARDOWN, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.TEARDOWN;
		}
		if (hex.StartsWith(RequestConst.PAUSE, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.PAUSE;
		}
		if (hex.StartsWith(RequestConst.SETPEERS, StringComparison.OrdinalIgnoreCase))
		{
			return RequestType.SETPEERS;
		}

		return null;
	}

	#endregion
}