#region References

using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabGrids : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Grids";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	public TabGrids() : this(
		CornerstoneApplication.GetInstance<IDateTimeProvider>(),
		CornerstoneApplication.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabGrids(IDateTimeProvider dateTimeProvider, IRuntimeInformation runtimeInformation)
	{
		_dateTimeProvider = dateTimeProvider;

		Blocks =
		[
			new Block { Text = "Red", Column = 0, Row = 0, Color = Brushes.Red },
			new Block { Text = "Green", Column = 1, Row = 0, Color = Brushes.Green },
			new Block { Text = "Blue", Column = 0, Row = 1, Color = Brushes.Blue },
			new Block { Text = "Violet", Column = 1, Row = 1, Color = Brushes.BlueViolet }
		];
		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public PresentationList<Block> Blocks { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}

[Notifiable(["*"])]
public partial class Block : Notifiable
{
	#region Properties

	public partial ISolidColorBrush Color { get; set; }
	public partial int Column { get; set; }
	public partial int Row { get; set; }
	public partial string Text { get; set; }

	#endregion
}