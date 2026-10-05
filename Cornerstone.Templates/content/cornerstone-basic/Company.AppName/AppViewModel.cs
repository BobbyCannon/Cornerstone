#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

[SourceReflection]
public partial class AppViewModel : ApplicationViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher)
		: base(dependencyProvider, dispatcher)
	{
		Greeting = "Welcome to Cornerstone";
	}

	#endregion

	#region Properties

	[Notify]
	public partial string Greeting { get; private set; }

	#endregion
}
