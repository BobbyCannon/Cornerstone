#region References

using System.ComponentModel;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.TestAssembly;

public class Account : INotifyPropertyChanged
{
	#region Methods

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}