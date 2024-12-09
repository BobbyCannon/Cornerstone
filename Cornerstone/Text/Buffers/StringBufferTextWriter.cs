#region References

using System.IO;
using System.Text;

#endregion

namespace Cornerstone.Text.Buffers;

public class StringBufferTextWriter : TextWriter
{
	#region Fields

	private readonly IStringBuffer _buffer;

	#endregion

	#region Constructors

	public StringBufferTextWriter(IStringBuffer buffer)
	{
		_buffer = buffer;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override Encoding Encoding => Encoding.UTF8;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Write(char value)
	{
		_buffer.Add(value);
	}

	/// <inheritdoc />
	public override void Write(char[] buffer)
	{
		_buffer.Add(buffer);
	}

	#endregion
}