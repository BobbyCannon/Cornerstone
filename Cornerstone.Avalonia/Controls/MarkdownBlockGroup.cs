#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Parsers;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// A visual grouping of one or more markdown <see cref="Block" />s (a paragraph of inlines,
/// or a single block-level node such as a code fence / header).
/// </summary>
[SourceReflection]
public partial class MarkdownBlockGroup
{
	#region Fields

	private readonly List<Block> _blocks;

	#endregion

	#region Constructors

	public MarkdownBlockGroup()
	{
		_blocks = new(16);
		Links = new List<MarkdownProjectedLink>(4);

		ContentBuffer = new StringBuffer();
		CopyRange = new TextRange(0, 0);
	}

	#endregion

	#region Properties

	public IReadOnlyList<Block> Blocks => _blocks;

	public StringBuffer ContentBuffer { get; }

	/// <summary>
	/// Incremented when the group content changes in place so presenters can refresh.
	/// </summary>
	public int ContentVersion { get; private set; }

	public TextRange CopyRange { get; }

	public int EndOffset { get; private set; }

	/// <summary>
	/// True when this is a single block-level construct (header, code, quote, table, list).
	/// </summary>
	public bool IsBlockLevel { get; private set; }

	/// <summary>
	/// True when the group ends at EOF and may still be growing (e.g. open code fence).
	/// </summary>
	public bool IsIncomplete { get; private set; }

	/// <summary>
	/// True when this group is a GFM table (structured table presenter).
	/// </summary>
	public bool IsTable =>
		IsBlockLevel
		&& (_blocks.Count == 1)
		&& (_blocks[0].Type == MarkdownTokenizer.TokenTypeTable);

	/// <summary>
	/// Projected (display-buffer) link spans for this group after the last content apply.
	/// </summary>
	public List<MarkdownProjectedLink> Links { get; }

	/// <summary>
	/// Primary token type for block-level groups; otherwise first block type.
	/// </summary>
	public int PrimaryType { get; private set; }

	public int StartOffset { get; private set; }

	#endregion

	#region Methods

	public void Add(Block block)
	{
		if (_blocks.Count == 0)
		{
			StartOffset = block.StartOffset;
			PrimaryType = block.Type;
			IsBlockLevel = MarkdownView.IsBlockLevel(block);
		}

		_blocks.Add(block);
		EndOffset = block.EndOffset;
	}

	public void FinalizeIncomplete(int documentLength)
	{
		IsIncomplete = IsBlockLevel
			&& (EndOffset >= documentLength)
			&& (_blocks.Count > 0);
	}

	/// <summary>
	/// True when <paramref name="other" /> is the same construct growing/shrinking at the end
	/// (same start and type) — typical open code fence during streaming.
	/// </summary>
	public bool MatchesGrowingTail(MarkdownBlockGroup other)
	{
		return other is not null
			&& IsBlockLevel
			&& other.IsBlockLevel
			&& (PrimaryType == other.PrimaryType)
			&& (StartOffset == other.StartOffset)
			&& (_blocks.Count == 1)
			&& (other._blocks.Count == 1);
	}

	/// <summary>
	/// Structural equality for reconcile: same types and exact ranges.
	/// </summary>
	public bool MatchesStructure(MarkdownBlockGroup other)
	{
		if (other is null
			|| (_blocks.Count != other._blocks.Count)
			|| (IsBlockLevel != other.IsBlockLevel)
			|| (PrimaryType != other.PrimaryType)
			|| (StartOffset != other.StartOffset))
		{
			return false;
		}

		for (var i = 0; i < _blocks.Count; i++)
		{
			var a = _blocks[i];
			var b = other._blocks[i];
			if ((a.Type != b.Type)
				|| (a.StartOffset != b.StartOffset)
				|| (a.EndOffset != b.EndOffset))
			{
				return false;
			}
		}

		return true;
	}

	public void RemoveAt(int index)
	{
		_blocks.RemoveAt(index);
		if (_blocks.Count == 0)
		{
			StartOffset = 0;
			EndOffset = 0;
			return;
		}

		EndOffset = _blocks[^1].EndOffset;
	}

	public void Reset()
	{
		_blocks.Clear();
		Links.Clear();
		ContentBuffer.Clear();
		CopyRange.StartOffset = 0;
		CopyRange.EndOffset = 0;
		StartOffset = 0;
		EndOffset = 0;
		PrimaryType = 0;
		IsBlockLevel = false;
		IsIncomplete = false;
		ContentVersion = 0;
	}

	/// <summary>
	/// Clears blocks, optionally returning them to <paramref name="blockPool" />.
	/// </summary>
	public void Reset(IQueue<Block> blockPool)
	{
		if (blockPool is not null)
		{
			foreach (var block in _blocks)
			{
				blockPool.Enqueue(block);
			}
		}

		Reset();
	}

	/// <summary>
	/// Copies block data from <paramref name="source" /> into this instance (keeps object identity for UI).
	/// Previous blocks are returned to <paramref name="blockPool" /> when provided.
	/// </summary>
	public void UpdateFrom(MarkdownBlockGroup source, int documentLength, IQueue<Block> blockPool = null)
	{
		if (blockPool is not null)
		{
			foreach (var block in _blocks)
			{
				blockPool.Enqueue(block);
			}
		}

		_blocks.Clear();
		foreach (var block in source._blocks)
		{
			_blocks.Add(block);
		}

		// Source no longer owns these blocks
		source._blocks.Clear();

		StartOffset = source.StartOffset;
		EndOffset = source.EndOffset;
		PrimaryType = source.PrimaryType;
		IsBlockLevel = source.IsBlockLevel;
		IsIncomplete = source.IsIncomplete || ((EndOffset >= documentLength) && IsBlockLevel);
		ContentVersion++;
		ContentChanged?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler ContentChanged;

	#endregion
}