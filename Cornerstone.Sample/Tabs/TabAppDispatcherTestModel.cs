#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs;

public interface ITabAppDispatcherTestModel
{
	#region Properties

	int Number { get; }

	#endregion
}

/// <summary>
/// Simulates the Model of MVVM (Keystone State slice) for the Automatic demo.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class TabAppDispatcherTestModel : CornerstoneObject, ITabAppDispatcherTestModel
{
	#region Properties

	public partial int Number { get; set; }

	#endregion
}