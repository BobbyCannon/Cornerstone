#region References

using System;
using System.Globalization;
using Cornerstone.Collections;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// A segment using <see cref="TextAnchor" />s as start and end positions.
/// </summary>
/// <remarks>
/// <para>
/// For the constructors creating new anchors, the start position will be AfterInsertion and the end position will be BeforeInsertion.
/// Should the end position move before the start position, the segment will have length 0.
/// </para>
/// </remarks>
/// <seealso cref="IRange" />
/// <seealso cref="TextRange" />
public sealed class AnchorRange : IRange
{
	#region Fields

	private readonly TextAnchor _end;
	private readonly TextAnchor _start;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new AnchorSegment using the specified anchors.
	/// The anchors must have <see cref="TextAnchor.SurviveDeletion" /> set to true.
	/// </summary>
	public AnchorRange(TextAnchor start, TextAnchor end)
	{
		if (start == null)
		{
			throw new ArgumentNullException(nameof(start));
		}
		if (end == null)
		{
			throw new ArgumentNullException(nameof(end));
		}
		if (!start.SurviveDeletion)
		{
			throw new ArgumentException("Anchors for AnchorSegment must use SurviveDeletion", nameof(start));
		}
		if (!end.SurviveDeletion)
		{
			throw new ArgumentException("Anchors for AnchorSegment must use SurviveDeletion", nameof(end));
		}
		_start = start;
		_end = end;
	}

	/// <summary>
	/// Creates a new AnchorSegment that creates new anchors.
	/// </summary>
	public AnchorRange(TextEditorDocument document, IRange range)
		: this(document, ThrowUtil.CheckNotNull(range, "segment").StartIndex, range.Length)
	{
	}

	/// <summary>
	/// Creates a new AnchorSegment that creates new anchors.
	/// </summary>
	public AnchorRange(TextEditorDocument document, int offset, int length)
	{
		_start = document?.CreateAnchor(offset) ?? throw new ArgumentNullException(nameof(document));
		_start.SurviveDeletion = true;
		_start.MovementType = AnchorMovementType.AfterInsertion;
		_end = document.CreateAnchor(offset + length);
		_end.SurviveDeletion = true;
		_end.MovementType = AnchorMovementType.BeforeInsertion;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int EndIndex => Math.Max(_start.Offset, _end.Offset);

	/// <inheritdoc />
	public int Length => Math.Max(0, _end.Offset - _start.Offset);

	/// <inheritdoc />
	public int StartIndex => _start.Offset;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return "[Offset=" + StartIndex.ToString(CultureInfo.InvariantCulture) + ", EndOffset=" + EndIndex.ToString(CultureInfo.InvariantCulture) + "]";
	}

	#endregion
}