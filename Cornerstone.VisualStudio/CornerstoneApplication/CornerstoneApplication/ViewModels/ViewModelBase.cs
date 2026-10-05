#region References

using System.ComponentModel;
using System.Runtime.CompilerServices;

#endregion

namespace CornerstoneApplication.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion

	#region Methods

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}
