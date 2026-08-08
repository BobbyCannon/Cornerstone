#region References

using Avalonia.Platform;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Camera;

internal class CameraAdapterStub : BaseCameraAdapter
{
	#region Constructors

	public CameraAdapterStub(IDispatcher dispatcher) : base(dispatcher)
	{
		AvailableModes = new PresentationList<CameraMode>(dispatcher) { CameraMode.Video };
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override IPresentationList<CameraMode> AvailableModes { get; }

	/// <inheritdoc />
	public override IPlatformHandle PlatformHandle { get; }

	#endregion
}