#region References

using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.Controls.Camera;

internal class CameraAdapterStub : BaseCameraAdapter
{
	#region Constructors

	public CameraAdapterStub() : base(null)
	{
		AvailableModes = new PresentationList<CameraMode> { CameraMode.Video };
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override IPresentationList<CameraMode> AvailableModes { get; }

	/// <inheritdoc />
	public override IPlatformHandle PlatformHandle { get; }

	#endregion
}