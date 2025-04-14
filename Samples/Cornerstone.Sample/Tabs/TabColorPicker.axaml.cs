#region References

using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabColorPicker : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Color Picker";

	#endregion

	#region Fields

	private readonly IRuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	public TabColorPicker()
		: this(GetInstance<IRuntimeInformation>(),
			GetInstance<IDispatcher>())
	{
	}

	[DependencyInjectionConstructor]
	public TabColorPicker(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(dispatcher)
	{
		_runtimeInformation = runtimeInformation;

		DataContext = this;

		InitializeComponent();
	}

	#endregion
}