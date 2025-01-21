#region References

using System.Windows.Input;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabRelayCommand : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "RelayCommand";

	#endregion

	#region Constructors

	public TabRelayCommand()
	{
		ProcessCommand = new RelayCommand(Process, _ => CanExecute);
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool CanExecute { get; set; }

	public ICommand ProcessCommand { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(CanExecute):
			{
				ProcessCommand.Refresh();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	private void Process(object obj)
	{
		Editor.Document.Insert(0, $"{DateTimeProvider.RealTime.UtcNow}\r\n");
	}

	#endregion
}