#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Model list row for the Collections demo (shared type on model + view lists).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class TabAppDispatcherSampleCollectionItem : CornerstoneObject
{
	#region Constructors

	public TabAppDispatcherSampleCollectionItem(int id, string name, int score)
	{
		Id = id;
		Name = name;
		Score = score;
	}

	#endregion

	#region Properties

	public partial int Id { get; set; }

	public partial string Name { get; set; }

	public partial int Score { get; set; }

	#endregion
}