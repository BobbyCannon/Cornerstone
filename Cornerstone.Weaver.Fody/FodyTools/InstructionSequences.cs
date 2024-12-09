#region References

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

/// <summary>
/// Group instructions by sequence points.
/// </summary>
internal class InstructionSequences : ReadOnlyCollection<InstructionSequence>
{
	#region Constructors

	public InstructionSequences(IList<Instruction> instructions, IList<SequencePoint> sequencePoints)
		: base(CreateSequences(instructions, sequencePoints).ToList())
	{
		Instructions = instructions;
	}

	#endregion

	#region Properties

	public IList<Instruction> Instructions { get; }

	#endregion

	#region Methods

	private static IEnumerable<InstructionSequence> CreateSequences(IList<Instruction> instructions, IList<SequencePoint> sequencePoints)
	{
		if (sequencePoints?.Any() != true)
		{
			yield return new InstructionSequence(instructions, null, instructions.Count, null);
			yield break;
		}

		var sequencePointMapper = new SequencePointMapper(sequencePoints);

		var sequences = instructions
			.Select(inst => sequencePointMapper.GetNext(inst.Offset))
			.GroupBy(item => item);

		InstructionSequence previous = null;

		foreach (var group in sequences)
		{
			yield return previous = new InstructionSequence(instructions, previous, group.Count(), group.Key);
		}
	}

	#endregion

	#region Classes

	private class SequencePointMapper
	{
		#region Fields

		private int _index = 1;
		private readonly IList<SequencePoint> _sequencePoints;

		#endregion

		#region Constructors

		public SequencePointMapper(IList<SequencePoint> sequencePoints)
		{
			_sequencePoints = sequencePoints;
		}

		#endregion

		#region Methods

		public SequencePoint GetNext(int offset)
		{
			while (true)
			{
				if (_index >= _sequencePoints.Count)
				{
					return _sequencePoints.Last();
				}

				var nextPoint = _sequencePoints[_index];

				if (nextPoint.Offset > offset)
				{
					return _sequencePoints[_index - 1];
				}

				_index += 1;
			}
		}

		#endregion
	}

	#endregion
}