#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

#endregion

#pragma warning disable S112 // General exceptions should never be thrown => in indexers IndexOutOfRange exception is expected.

namespace Cornerstone.Weaver.Fody.FodyTools;

/// <summary>
/// A list of instructions belonging to the same sequence point. This is just a view wrapping the original list, modifications will be forwarded to the original list.
/// </summary>
internal class InstructionSequence : IList<Instruction>
{
	#region Fields

	private readonly IList<Instruction> _instructions;
	private readonly InstructionSequence _previous;

	#endregion

	#region Constructors

	public InstructionSequence(IList<Instruction> instructions, InstructionSequence previous, int count, SequencePoint point)
	{
		_instructions = instructions;
		_previous = previous;
		Count = count;
		Point = point;
	}

	#endregion

	#region Properties

	public int Count { get; private set; }

	public Instruction this[int index]
	{
		get
		{
			if ((index < 0) || (index >= Count))
			{
				throw new IndexOutOfRangeException();
			}

			var startIndex = StartIndex;

			return _instructions[startIndex + index];
		}
		set
		{
			if ((index < 0) || (index >= Count))
			{
				throw new IndexOutOfRangeException();
			}

			var startIndex = StartIndex;

			var oldValue = _instructions[startIndex + index];
			_instructions[startIndex + index] = value;

			foreach (var instr in _instructions)
			{
				if (instr.Operand == oldValue)
				{
					instr.Operand = value;
				}
			}
		}
	}

	public SequencePoint Point { get; }

	bool ICollection<Instruction>.IsReadOnly => false;

	private int NextStartIndex => StartIndex + Count;

	private int StartIndex => _previous?.NextStartIndex ?? 0;

	#endregion

	#region Methods

	public void Add(Instruction item)
	{
		var startIndex = StartIndex;

		_instructions.Insert(startIndex + Count, item!);

		Count += 1;
	}

	public bool Contains(Instruction item)
	{
		var startIndex = StartIndex;

		var itemIndex = _instructions.IndexOf(item!);

		return (itemIndex >= startIndex) && (itemIndex < (startIndex + Count));
	}

	public int IndexOf(Instruction item)
	{
		var index = _instructions.IndexOf(item!);
		var startIndex = StartIndex;

		if ((index >= startIndex) && (index < (startIndex + Count)))
		{
			return index - startIndex;
		}

		return -1;
	}

	public void Insert(int index, Instruction item)
	{
		if ((index < 0) || (index > Count))
		{
			throw new IndexOutOfRangeException();
		}

		var startIndex = StartIndex;
		_instructions.Insert(index + startIndex, item!);
		Count += 1;
	}

	public bool Remove(Instruction item)
	{
		var startIndex = StartIndex;

		var itemIndex = _instructions.IndexOf(item!);

		if ((itemIndex < startIndex) || (itemIndex >= (startIndex + Count)))
		{
			return false;
		}

		_instructions.RemoveAt(itemIndex);
		Count -= 1;
		return true;
	}

	public void RemoveAt(int index)
	{
		if ((index < 0) || (index >= Count))
		{
			throw new IndexOutOfRangeException();
		}

		var startIndex = StartIndex;

		_instructions.RemoveAt(index + startIndex);
		Count -= 1;
	}

	void ICollection<Instruction>.Clear()
	{
		throw new NotImplementedException();
	}

	void ICollection<Instruction>.CopyTo(Instruction[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException(nameof(array));
		}
		if (arrayIndex < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException("array is multidimensional", nameof(array));
		}
		if (array.Length < (arrayIndex + Count))
		{
			throw new ArgumentException("array is too short", nameof(array));
		}

		var startIndex = StartIndex;

		for (var i = 0; i < Count; i++)
		{
			array[arrayIndex++] = _instructions[startIndex + i];
		}
	}

	IEnumerator<Instruction> IEnumerable<Instruction>.GetEnumerator()
	{
		var startIndex = StartIndex;

		return _instructions.Skip(startIndex).Take(Count).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<Instruction>) this).GetEnumerator();
	}

	#endregion
}