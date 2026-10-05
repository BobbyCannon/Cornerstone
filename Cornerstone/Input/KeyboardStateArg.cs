#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Input;

[SourceReflection]
[Updateable(UpdateableAction.All, ["*"])]
public partial class KeyboardStateArg : KeyboardState,
	IUpdateable<KeyboardStateArg>,
	IUpdateable<KeyboardState>
{
	#region Properties

	public bool IsHandled { get; set; }

	#endregion
}