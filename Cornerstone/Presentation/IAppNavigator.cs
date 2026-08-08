#region References

using System.Windows.Input;

#endregion

namespace Cornerstone.Presentation;

public interface IAppNavigator
{
	#region Properties

	ICommand SelectViewCommand { get; }

	#endregion

	#region Methods

	bool TryToSelectViewByModel(string assemblyName);

	#endregion
}