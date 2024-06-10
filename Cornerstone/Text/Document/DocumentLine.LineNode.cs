namespace Cornerstone.Text.Document;

// A tree node in the document line tree.
// For the purpose of the invariants, "children", "descendents", "siblings" etc. include the DocumentLine object,
// it is treated as a third child node between left and right.
partial class DocumentLine
{
	#region Properties

	/// <summary>
	/// optimization note: I tried packing color and isDeleted into a single byte field, but that
	/// actually increased the memory requirements. The JIT packs two bools and a byte (delimiterSize)
	/// into a single DWORD, but two bytes get each their own DWORD. Three bytes end up in the same DWORD, so
	/// apparently the JIT only optimizes for memory when there are at least three small fields.
	/// Currently, DocumentLine takes 36 bytes on x86 (8 byte object overhead, 3 pointers, 3 ints, and another DWORD
	/// for the small fields).
	/// TODO: a possible optimization would be to combine 'totalLength' and the small fields into a single uint.
	/// delimiterSize takes only two bits, the two bools take another two bits; so there's still 
	/// 28 bits left for totalLength. 268435455 characters per line should be enough for everyone :)
	/// </summary>
	internal bool Color { get; set; }

	internal DocumentLine Left { get; set; }

	internal DocumentLine LeftMost
	{
		get
		{
			var node = this;
			while (node.Left != null)
			{
				node = node.Left;
			}
			return node;
		}
	}

	/// <summary>
	/// The number of lines in this node and its child nodes.
	/// Invariant:
	/// nodeTotalCount = 1 + left.nodeTotalCount + right.nodeTotalCount
	/// </summary>
	internal int NodeTotalCount { get; set; }

	/// <summary>
	/// The total text length of this node and its child nodes.
	/// Invariant:
	/// nodeTotalLength = left.nodeTotalLength + documentLine.TotalLength + right.nodeTotalLength
	/// </summary>
	internal int NodeTotalLength { get; set; }

	internal DocumentLine Parent { get; set; }

	internal DocumentLine Right { get; set; }

	internal DocumentLine RightMost
	{
		get
		{
			var node = this;
			while (node.Right != null)
			{
				node = node.Right;
			}
			return node;
		}
	}

	#endregion

	#region Methods

	internal DocumentLine InitLineNode()
	{
		NodeTotalCount = 1;
		NodeTotalLength = TotalLength;
		return this;
	}

	/// <summary>
	/// Resets the line to enable its reuse after a document rebuild.
	/// </summary>
	internal void ResetLine()
	{
		_totalLength = _delimiterLength = 0;
		_isDeleted = Color = false;

		Left = Right = Parent = null;
	}

	#endregion
}