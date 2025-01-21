#region References

using Avalonia.Controls;

#endregion

namespace Cornerstone.Sample.ViewModels;

public class TabItemViewModel
{
	#region Constructors

	public TabItemViewModel(string header, UserControl content)
	{
		Header = header;
		Content = content;
	}

	#endregion

	#region Properties

	public UserControl Content { get; }

	public string Header { get; }

	#endregion
}