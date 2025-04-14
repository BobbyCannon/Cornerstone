#region References

using System.Windows.Input;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Runtime;

public class Permission : Bindable
{
	#region Constructors

	internal Permission(Permissions permissionsManager)
		: base(permissionsManager.GetDispatcher())
	{
		PermissionsManager = permissionsManager;

		RequestPermissionCommand = new RelayCommand(_ => RequestPermission());
	}

	#endregion

	#region Properties

	public Permissions PermissionsManager { get; }

	public ICommand RequestPermissionCommand { get; }

	public PermissionStatus Status { get; set; }

	public PermissionType Type { get; set; }

	#endregion

	#region Methods

	private async void RequestPermission()
	{
		await PermissionsManager.RequestPermissionAsync(Type);
	}

	#endregion
}