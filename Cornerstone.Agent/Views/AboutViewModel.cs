#region References

using Cornerstone.Agent.Keystone;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
[DependencyInjected]
public partial class AboutViewModel : ViewModel
{
	#region Fields

	public static readonly string AssemblyName;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AboutViewModel(AppBus bus, AppState state, IAppNavigator navigator)
	{
		Bus = bus;
		State = state;
		Navigator = navigator;
	}

	static AboutViewModel()
	{
		AssemblyName = typeof(AboutViewModel).ToAssemblyName();
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }
	public IAppNavigator Navigator { get; }
	public AppState State { get; }

	#endregion
}