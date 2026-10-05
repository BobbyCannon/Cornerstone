#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.AppDispatcher;

[SourceReflection]
public partial class TabAppDispatcherTestView : UserControl<TabAppDispatcherTestViewModel>
{
	#region Constructors

	public TabAppDispatcherTestView()
	{
		InitializeComponent();
	}

	#endregion
}