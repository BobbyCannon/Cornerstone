#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core.Completion;

public class CompletionSet
{
	#region Constructors

	public CompletionSet()
	{
		Completions = [];
	}

	#endregion

	#region Properties

	public List<Completion> Completions { get; set; }

	public int StartPosition { get; set; }

	#endregion
}