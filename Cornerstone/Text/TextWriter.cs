#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// Provides high-performance utilities for reading text in an IStringBuffer (including gap buffers).
/// </summary>
[SourceReflection]
public class TextWriter
{
	#region Constructors

	public TextWriter(IStringBuffer buffer, ITextSettings settings)
	{
		Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		Settings = settings;
	}

	#endregion

	#region Properties

	public uint Indent { get; set; }

	public virtual ITextSettings Settings { get; }

	protected IStringBuffer Buffer { get; }

	#endregion

	#region Methods

	public void Append(string value)
	{
		Buffer.Append(value);
	}

	public void AppendLine()
	{
		Buffer.Append(Settings.NewLineChars);
	}

	public void AppendLine(string value)
	{
		Buffer.Append(value);
		Buffer.Append(Settings.NewLineChars);
	}

	public virtual void Clear()
	{
		Indent = 0;
	}

	public void IncreaseIndent()
	{
		Indent += Settings.IndentLength;
	}

	public void IndentWrite(string value)
	{
		WriteIndent();
		Buffer.Append(value);
	}

	public void IndentWriteLine(char value)
	{
		WriteIndent();
		Buffer.Append(value);
		Buffer.Append(Settings.NewLineChars);
	}

	public void IndentWriteLine(string value)
	{
		WriteIndent();
		Buffer.Append(value);
		Buffer.Append(Settings.NewLineChars);
	}

	protected internal void WriteIndent()
	{
		if (Indent == 0)
		{
			return;
		}

		for (var i = 0; i < Indent; i++)
		{
			Buffer.Append(Settings.IndentChar);
		}
	}

	public void DecreaseIndent()
	{
		if (Indent >= Settings.IndentLength)
		{
			Indent -= Settings.IndentLength;
		}
	}

	#endregion
}