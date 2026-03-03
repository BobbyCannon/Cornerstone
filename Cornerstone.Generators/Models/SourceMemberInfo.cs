#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Generators.Models;

public abstract class SourceMemberInfo
{
	#region Fields

	public readonly List<SourceAttributeInfo> Attributes = [];
	public string Name;

	#endregion
}