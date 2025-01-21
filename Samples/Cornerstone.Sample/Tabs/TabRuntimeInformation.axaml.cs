#region References

using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sample.ViewModels;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabRuntimeInformation : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Runtime Information";

	#endregion

	#region Constructors

	public TabRuntimeInformation() : this(DesignModeDependencyProvider.Get<IRuntimeInformation>(), null)
	{
	}

	[DependencyInjectionConstructor]
	public TabRuntimeInformation(IRuntimeInformation runtimeInformation, IDispatcher dispatcher) : base(dispatcher)
	{
		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}