#region References

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.RemoteLink.AirPlay.Models.Enums;

#endregion

namespace Cornerstone.RemoteLink.AirPlay.Models.TcpListeners;

public class Response
{
	#region Fields

	private readonly MemoryStream _responseStream;

	#endregion

	#region Constructors

	public Response()
	{
		_responseStream = new MemoryStream();
		Headers = new HeadersCollection();
	}

	public Response(ProtocolType protocol) : this()
	{
		Protocol = protocol;
	}

	public Response(ProtocolType protocol, StatusCode statusCode) : this(protocol)
	{
		StatusCode = statusCode;
	}

	#endregion

	#region Properties

	public HeadersCollection Headers { get; }

	public ProtocolType Protocol { get; set; }

	public StatusCode StatusCode { get; set; } = StatusCode.OK;

	#endregion

	#region Methods

	public string GetProtocol()
	{
		switch (Protocol)
		{
			case ProtocolType.HTTP10:
				return "HTTP/1.0";
			case ProtocolType.HTTP11:
				return "HTTP/1.1";
			case ProtocolType.RTSP10:
				return "RTSP/1.0";
			default:
				return null;
		}
	}

	public Task<byte[]> ReadAsync()
	{
		return Task.FromResult(_responseStream.ToArray());
	}

	public Task WriteAsync(byte[] buffer)
	{
		return WriteAsync(buffer, 0, buffer.Length);
	}

	public Task WriteAsync(byte[] buffer, int index, int count)
	{
		using (var writer = new BinaryWriter(_responseStream, Encoding.ASCII))
		{
			writer.Write(buffer, index, count);
		}

		Headers.Add("Content-Length", count.ToString());

		return Task.CompletedTask;
	}

	#endregion
}