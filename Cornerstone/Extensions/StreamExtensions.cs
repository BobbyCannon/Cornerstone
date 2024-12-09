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
	/// Calculate an MD5 hash for the stream.
	/// </summary>
	/// <param name="input"> The stream to hash. </param>
	/// <returns> The MD5 formatted hash for the input. </returns>
	public static string CalculateHash(this Stream input)
	{
		// Calculate MD5 hash from input.
		var md5 = MD5.Create();
		var hash = md5.ComputeHash(input);

		// Convert byte array to hex string.
		var sb = new StringBuilder();
		foreach (var item in hash)
		{
			sb.Append(item.ToString("X2"));
		}

		// Return the MD5 string.
		return sb.ToString().ToLower();
	}

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