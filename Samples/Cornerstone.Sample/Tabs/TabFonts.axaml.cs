#region References

using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabFonts : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Fonts";

	#endregion

	#region Constructors

	public TabFonts() : this(null)
	{
	}

	[DependencyInjectionConstructor]
	public TabFonts(IDispatcher dispatcher) : base(dispatcher)
	{
		DataContext = this;

		InitializeComponent();
	}

	#endregion
}