#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Collections;

[Notifiable(["*"])]
[SourceReflection]
[Updateable(UpdateableAction.All, ["*"])]
public partial class Range : CornerstoneObject, IRange
{
	#region Properties

	[AlsoNotify(nameof(Length))]
	public partial int EndOffset { get; set; }

	public int Length => EndOffset - StartOffset;

	[AlsoNotify(nameof(Length))]
	public partial int StartOffset { get; set; }

	#endregion
}

public interface IRange
{
	#region Properties

	/// <summary>
	/// The exclusive end offset (StartOffset + Length).
	/// </summary>
	int EndOffset { get; }

	/// <summary>
	/// The length of the section.
	/// </summary>
	int Length { get; }

	/// <summary>
	/// The inclusive start offset.
	/// </summary>
	int StartOffset { get; }

	#endregion
}