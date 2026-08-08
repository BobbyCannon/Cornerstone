#region References

using Cornerstone.Avalonia;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
public partial class AgentView : CornerstoneUserControl<AgentViewModel>
{
	#region Constructors

	public AgentView()
	{
		InitializeComponent();
	}

	#endregion
}