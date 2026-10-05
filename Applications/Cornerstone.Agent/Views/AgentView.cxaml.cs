#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Theme;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
public partial class AgentView : UserControl<AgentViewModel>
{
	#region Constructors

	public AgentView()
	{
		InitializeComponent();
	}

	#endregion
}