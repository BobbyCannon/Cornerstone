#region References

using System.ComponentModel;

#endregion

namespace Cornerstone.Weaver.TestAssembly;

public class AccountINotifyPropertyChanged : INotifyPropertyChanged, IAccount
{
	#region Properties

	public int Age { get; set; }

	public string FirstName { get; set; }

	public string FullName => $"{FirstName} {LastName}";

	public string LastName { get; set; }

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}