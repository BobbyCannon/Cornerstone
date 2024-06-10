#region References

using System;
using System.Diagnostics;
using System.Globalization;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Represents a simple segment (Offset,Length pair) that is not automatically updated
/// on document changes.
/// </summary>
public struct SimpleSegment : IEquatable<SimpleSegment>, ISegment
{
	#region Fields

	public readonly int Offset, Length;

	#endregion

	#region Constructors

	public SimpleSegment(int offset, int length)
	{
		Offset = offset;
		Length = length;
	}

	public SimpleSegment(ISegment segment)
	{
		Debug.Assert(segment != null);
		Offset = segment.Offset;
		Length = segment.Length;
	}

	#endregion

	#region Properties

	public int EndOffset => Offset + Length;

	int ISegment.Length => Length;

	int ISegment.Offset => Offset;

	#endregion

	#region Methods

	public override bool Equals(object obj)
	{
		return obj is SimpleSegment && Equals((SimpleSegment) obj);
	}

	public bool Equals(SimpleSegment other)
	{
		return (Offset == other.Offset) && (Length == other.Length);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return Offset + (10301 * Length);
		}
	}

	public static bool operator ==(SimpleSegment left, SimpleSegment right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(SimpleSegment left, SimpleSegment right)
	{
		return !left.Equals(right);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[Offset={Offset}, Length={Length}]";
	}

	#endregion
}

/// <summary>
/// A segment using <see cref="TextAnchor" />s as start and end positions.
/// </summary>
/// <remarks>
/// <para>
/// For the constructors creating new anchors, the start position will be AfterInsertion and the end position will be BeforeInsertion.
/// Should the end position move before the start position, the segment will have length 0.
/// </para>
/// </remarks>
/// <seealso cref="ISegment" />
/// <seealso cref="TextSegment" />
public sealed class AnchorSegment : ISegment
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
	public AnchorSegment(TextAnchor start, TextAnchor end)
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
	public AnchorSegment(TextDocument document, ISegment segment)
		: this(document, ThrowUtil.CheckNotNull(segment, "segment").Offset, segment.Length)
	{
	}

	/// <summary>
	/// Creates a new AnchorSegment that creates new anchors.
	/// </summary>
	public AnchorSegment(TextDocument document, int offset, int length)
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
	public int EndOffset => Math.Max(_start.Offset, _end.Offset);

	/// <inheritdoc />
	public int Length => Math.Max(0, _end.Offset - _start.Offset);

	/// <inheritdoc />
	public int Offset => _start.Offset;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", EndOffset=" + EndOffset.ToString(CultureInfo.InvariantCulture) + "]";
	}

	#endregion
}