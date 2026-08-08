#region References

using System.Windows.Controls;
using Cornerstone.VisualStudio.Services;

#endregion

namespace Cornerstone.VisualStudio.Views;

public partial class OptionsView : UserControl
{
	#region Constructors

	public OptionsView()
	{
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ICornerstoneSettings Settings
	{
		get => DataContext as ICornerstoneSettings;
		set => DataContext = value;
	}

	#endregion
}