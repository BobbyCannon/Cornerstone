#region References

using Cornerstone.Avalonia;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabAppDispatcherTestView : CornerstoneUserControl<TabAppDispatcherTestViewModel>
{
	#region Constructors

	public TabAppDispatcherTestView()
	{
		InitializeComponent();
	}

	#endregion
}