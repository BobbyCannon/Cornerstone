#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public abstract class PointerTestsBase : ScopedTestBase
{
	#region Methods

	protected static StubPointerDevice CreatePointerDeviceMock(
		IPointer pointer = null,
		PointerType pointerType = PointerType.Mouse)
	{
		if (pointer is null)
		{
			pointer = new StubPointer(pointerType);
		}

		return new StubPointerDevice(pointer, pointerType);
	}

	protected static PointerEventArgs CreatePointerMovedArgs(
		IInputRoot root, IInputElement source, Point? position = null)
	{
		return new PointerEventArgs(InputElement.PointerMovedEvent, source, new StubPointer(), root.RootElement,
			position ?? default, default, PointerPointProperties.None, KeyModifiers.None);
	}

	protected static RawPointerEventArgs CreateRawPointerArgs(
		IPointerDevice pointerDevice,
		TopLevel root,
		RawPointerEventType type,
		Point? position = default)
	{
		return new RawPointerEventArgs(pointerDevice, 0, root.PresentationSource, type, position ?? default, default);
	}

	protected static RawPointerEventArgs CreateRawPointerMovedArgs(
		IPointerDevice pointerDevice,
		TopLevel root,
		Point? position = null)
	{
		return new RawPointerEventArgs(pointerDevice, 0, root.PresentationSource, RawPointerEventType.Move,
			position ?? default, default);
	}

	protected static void SetMove(StubPointerDevice deviceMock, IInputRoot root, IInputElement element)
	{
		deviceMock.SetProcessRawEvent(_ => element.RaiseEvent(CreatePointerMovedArgs(root, element)));
	}

	private protected static TopLevel CreateInputRoot(IWindowImpl impl, Control child, IHitTester hitTester)
	{
		var root = new Window(impl)
		{
			Width = 100,
			Height = 100,
			Content = child,
			Template = new FuncControlTemplate<Window>((w, _) => new ContentPresenter { Content = w.Content }),
			HitTesterOverride = hitTester
		};
		root.Show();
		return root;
	}

	private protected static StubWindowImpl CreateTopLevelImplMock()
	{
		var impl = new StubWindowImpl();
		impl.PointToScreenHandler = p => new PixelPoint((int) p.X, (int) p.Y);
		impl.PointToClientHandler = p => new Point(p.X, p.Y);

		var screen1 = new MockScreen(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
		impl.Screens = new StubScreenImpl(screen1);

		return impl;
	}

	private protected static void SetHit(StubHitTester renderer, Control hit)
	{
		renderer.SetHit(hit);
	}

	#endregion

	#region Classes

	protected class TestPointer : Pointer
	{
		#region Fields

		internal int PlatformCaptureCalled;

		#endregion

		#region Constructors

		internal TestPointer(int id, PointerType type, bool isPrimary) : base(id, type, isPrimary)
		{
		}

		#endregion

		#region Properties

		internal List<IInputElement> PlatformCaptures { get; } = new();

		#endregion

		#region Methods

		protected override void PlatformCapture(IInputElement element)
		{
			PlatformCaptureCalled++;
			PlatformCaptures.Add(element);
		}

		#endregion
	}

	#endregion
}