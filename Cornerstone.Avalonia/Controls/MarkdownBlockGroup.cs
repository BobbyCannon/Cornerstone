#region References

using System.Collections.Generic;
using Cornerstone.Parsers;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class MarkdownBlockGroup
{
	#region Fields

	private readonly List<Block> _blocks;

	#endregion

	#region Constructors

	public MarkdownBlockGroup()
	{
		_blocks = new(16);

		Buffer = new StringBuffer();
	}

	#endregion

	#region Properties

	public IReadOnlyList<Block> Blocks => _blocks;

	public StringBuffer Buffer { get; }

	#endregion

	#region Methods

	public void Add(Block block)
	{
		_blocks.Add(block);
	}

	public void Reset()
	{
		_blocks.Clear();
		Buffer.Clear();
	}

	#endregion
}