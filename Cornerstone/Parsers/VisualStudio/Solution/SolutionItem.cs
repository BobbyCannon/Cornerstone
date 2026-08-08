#region References

using Cornerstone.Collections;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

public partial class SolutionItem : SpeedyTree<SolutionItem>
{
	#region Properties

	public object Data { get; set; }

	public SolutionItemType ItemType { get; set; }

	public int Level { get; set; }

	public string Name { get; set; }

	#endregion
}