#region References

using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Core.Completion;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

[Export]
public class CompletionEngineSource
{
	#region Constructors

	public CompletionEngineSource()
	{
		CompletionEngine = new CompletionEngine();
	}

	#endregion

	#region Properties

	public CompletionEngine CompletionEngine { get; }

	#endregion
}