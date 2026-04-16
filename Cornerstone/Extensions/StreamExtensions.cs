#region References

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace Cornerstone.Extensions;

public static class StreamExtensions
{
	#region Methods

	/// <summary>
	/// Read the stream to the end and return value then disposing of the StreamReader.
	/// </summary>
	/// <param name="stream"> The stream to process. </param>
	/// <returns> The data that was read. </returns>
	public static byte[] ReadByteArray(this Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentException("Resource not found, stream is empty", nameof(stream));
		}

		var buffer = new byte[16 * 1024];
		using var ms = new MemoryStream();
		int read;

		while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
		{
			ms.Write(buffer, 0, read);
		}

		return ms.ToArray();
	}

	/// <summary>
	/// Read the stream to the end and return value then disposing of the StreamReader.
	/// </summary>
	/// <param name="stream"> The stream to process. </param>
	/// <returns> The data that was read. </returns>
	public static string ReadString(this Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentException("Resource not found, stream is empty", nameof(stream));
		}

		using (var reader = new StreamReader(stream))
		{
			return reader.ReadToEnd();
		}
	}

	#endregion
}