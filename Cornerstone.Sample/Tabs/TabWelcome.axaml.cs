#region References

using Cornerstone.Avalonia;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabWelcome : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Welcome";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	public TabWelcome() : this(
		AppBootstrap.GetInstance<IDateTimeProvider>(),
		AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabWelcome(IDateTimeProvider dateTimeProvider, IRuntimeInformation runtimeInformation)
	{
		_dateTimeProvider = dateTimeProvider;

		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}