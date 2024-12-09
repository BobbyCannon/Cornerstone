#region References

using System.Runtime.CompilerServices;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Text;

public class TextRange : ITextRange
{
	#region Fields


	private readonly TextDocument _text;

	#endregion

	#region Constructors

	internal TextRange(TextDocument text, TextRange parent, int length)
	{
		_text = text;
		
		Previous = parent;
		if (Previous != null)
		{
			Previous.Next = this;
		}

		Length = length;
	}

	static TextRange()
	{
		Empty = new TextRange(null, null, 0);
	}

	#endregion

	#region Properties

	public static TextRange Empty { get; set; }

	internal TextRange Previous { get; set; }

	internal TextRange Next { get; set; }

	/// <inheritdoc />
	public int EndIndex => StartIndex + Length;

	/// <inheritdoc />
	public int Length { get; set; }

	/// <inheritdoc />
	public int Remaining => _text == null ? 0 : _text.Length - EndIndex;

	/// <inheritdoc />
	public int StartIndex => Previous?.EndIndex ?? 0;

	#endregion

	#region Methods

	/// <summary>
	/// Extends the range by the provided length.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextRange operator +(TextRange range, int length)
	{
		range.Length += length;
		return range;
	}

	/// <summary>
	/// Reduces the range by the provided length.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextRange operator -(TextRange range, int length)
	{
		range.Length -= length;
		return range;
	}
	
	/// <inheritdoc />
	public override string ToString()
	{
		return _text?.SubString(StartIndex, Length);
	}

	#endregion
}

public interface ITextRange : IRange
{
	#region Properties

	/// <summary>
	/// Returns the number of characters not yet parsed. This is equal to the length
	/// of the text being parsed, minus the current position.
	/// </summary>
	int Remaining { get; }

	#endregion
}