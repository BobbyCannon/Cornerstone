#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid;

public class ReferencePath
{
	#region Constructors

	public ReferencePath()
	{
		Paths = new();
	}

	#endregion

	#region Properties

	public int Count => Paths.Count;

	public Stack<string> Paths { get; }

	#endregion

	#region Methods

	public void BeginScope(string scope)
	{
		Paths.Push(scope);
	}

	public void EndScope()
	{
		Paths.Pop();
	}

	public override string ToString()
	{
		return string.Join(", ", Paths.ToArray());
	}

	#endregion
}