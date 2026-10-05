#region References

using System.Windows.Input;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Runtime;

public class Permission : CornerstoneObject
{
	#region Constructors

	internal Permission(Permissions permissionsManager)
	{
		PermissionsManager = permissionsManager;
		RequestPermissionCommand = new RelayCommand(_ => RequestPermission());
	}

	#endregion

	#region Properties

	public int DisplayOrder { get; set; }

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
