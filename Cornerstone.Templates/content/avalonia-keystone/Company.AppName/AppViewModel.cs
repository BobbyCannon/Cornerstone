#region References

using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

[SourceReflection]
public class AppViewModel : ApplicationViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher)
		: base(dependencyProvider, dispatcher)
	{
	}

	#endregion
}
