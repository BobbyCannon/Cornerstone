#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Composition;

[TestClass]
public class CompositionDrawingSurfaceTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public async Task UpdateBeforeDisposeInSeparateBatchesShouldDisposeSnapshotWithTheSurface()
	{
		// Baseline: when the update is processed in an earlier batch than the dispose,
		// the surface's Dispose() releases the stored snapshot. This already works and
		// must keep working with the disposed-guard in place.
		using var services = new CompositorTestServices();
		var compositor = services.Compositor;

		var feature = new FakeExternalObjectsFeature();
		var interop = new CompositionInterop(compositor, feature);
		var imported = interop.ImportImage(
			new PlatformHandle(new IntPtr(1), "Fake"),
			new PlatformGraphicsExternalImageProperties { Width = 1, Height = 1 });

		services.RunJobs();

		var surface = compositor.CreateDrawingSurface();

		var update = surface.UpdateAsync(imported);
		services.RunJobs();
		await update;

		var snapshot = feature.Image.LastSnapshot;
		CornerstoneTest.IsNotNull(snapshot);
		CornerstoneTest.IsFalse(snapshot.IsDisposed);

		surface.Dispose();
		services.RunJobs();

		CornerstoneTest.IsTrue(snapshot.IsDisposed);
	}

	[PresentationTestMethod]
	public async Task UpdateProcessedAfterDisposeShouldDisposeSnapshotInsteadOfOrphaningIt()
	{
		// A commit batch is processed on the render thread in serialization order:
		// the dispose list is written (and therefore processed) BEFORE queued server
		// jobs. This means the perfectly legal user-code order
		//     surface.UpdateAsync(image); surface.Dispose();
		// executes on the render thread as Dispose() -> UpdateWithAutomaticSync().
		// Without a disposed-guard, the update stores a fresh snapshot into the
		// already-disposed surface. Nothing ever disposes that ref again, so the
		// GPU-backed snapshot is left to the RefCountable.Ref<T> critical finalizer,
		// which performs GPU work (context MakeCurrent + native SKImage dispose) on
		// the finalizer thread and races the compositor render loop. See #21865.
		using var services = new CompositorTestServices();
		var compositor = services.Compositor;

		var feature = new FakeExternalObjectsFeature();
		var interop = new CompositionInterop(compositor, feature);
		var imported = interop.ImportImage(
			new PlatformHandle(new IntPtr(1), "Fake"),
			new PlatformGraphicsExternalImageProperties { Width = 1, Height = 1 });

		services.RunJobs();
		CornerstoneTest.IsTrue(((CompositionImportedGpuImage) imported).ImportCompleted.IsCompletedSuccessfully);

		var surface = compositor.CreateDrawingSurface();

		var update = surface.UpdateAsync(imported);
		surface.Dispose();

		services.RunJobs();
		await update;

		var snapshot = feature.Image.LastSnapshot;
		CornerstoneTest.IsNotNull(snapshot);
		CornerstoneTest.IsTrue(snapshot.IsDisposed, "The snapshot taken by an update processed after the surface was disposed " +
			"must be disposed on the render thread instead of being orphaned to the finalizer.");
	}

	#endregion

	#region Classes

	private class FakeExternalObjectsFeature : IExternalObjectsRenderInterfaceContextFeature
	{
		#region Properties

		public byte[] DeviceLuid => null;
		public byte[] DeviceUuid => null;
		public FakeImportedImage Image { get; } = new();

		public IReadOnlyList<string> SupportedImageHandleTypes => new[] { "Fake" };
		public IReadOnlyList<string> SupportedSemaphoreTypes => Array.Empty<string>();

		#endregion

		#region Methods

		public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(
			string imageHandleType)
		{
			return CompositionGpuImportedImageSynchronizationCapabilities.Automatic;
		}

		public IPlatformRenderInterfaceImportedImage ImportImage(IPlatformHandle handle,
			PlatformGraphicsExternalImageProperties properties)
		{
			return Image;
		}

		public IPlatformRenderInterfaceImportedImage ImportImage(ICompositionImportableSharedGpuContextImage image)
		{
			return Image;
		}

		public IPlatformRenderInterfaceImportedSemaphore ImportSemaphore(IPlatformHandle handle)
		{
			throw new NotSupportedException();
		}

		#endregion
	}

	private class FakeImportedImage : IPlatformRenderInterfaceImportedImage
	{
		#region Properties

		public TrackingBitmapImpl LastSnapshot { get; private set; } = null!;

		#endregion

		#region Methods

		public void Dispose()
		{
		}

		public IBitmapImpl SnapshotWithAutomaticSync()
		{
			return Snapshot();
		}

		public IBitmapImpl SnapshotWithKeyedMutex(uint acquireIndex, uint releaseIndex)
		{
			return Snapshot();
		}

		public IBitmapImpl SnapshotWithSemaphores(
			IPlatformRenderInterfaceImportedSemaphore waitForSemaphore,
			IPlatformRenderInterfaceImportedSemaphore signalSemaphore)
		{
			return Snapshot();
		}

		public IBitmapImpl SnapshotWithTimelineSemaphores(
			IPlatformRenderInterfaceImportedSemaphore waitForSemaphore, ulong waitForValue,
			IPlatformRenderInterfaceImportedSemaphore signalSemaphore, ulong signalValue)
		{
			return Snapshot();
		}

		private IBitmapImpl Snapshot()
		{
			return LastSnapshot = new TrackingBitmapImpl();
		}

		#endregion
	}

	private class TrackingBitmapImpl : IBitmapImpl
	{
		#region Properties

		public Vector Dpi => new(96, 96);
		public bool IsDisposed { get; private set; }
		public PixelSize PixelSize => new(1, 1);
		public int Version => 1;

		#endregion

		#region Methods

		public void Dispose()
		{
			IsDisposed = true;
		}

		public void Save(Stream stream, BitmapEncoderOptions options)
		{
			throw new NotSupportedException();
		}

		#endregion
	}

	#endregion
}