#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class TestRoot : Decorator, IFocusScope, ILayoutRoot, IStyleHost, ILogicalRoot, IPresentationSource, IInputRoot
{
	#region Fields

	private FocusManager _focusManager;
	private readonly NameScope _nameScope = new();

	#endregion

	#region Constructors

	public TestRoot()
	{
		Renderer = RendererMocks.CreateRenderer();
		HitTester = new NullHitTester();
		LayoutManager = new LayoutManager(this);
		IsVisible = true;
		KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
		SetPresentationSourceForRootVisual(this);
	}

	public TestRoot(Control child)
		: this(false, child)
	{
	}

	public TestRoot(bool useGlobalStyles, Control child)
		: this()
	{
		if (useGlobalStyles)
		{
			StylingParent = UnitTestApplication.Current;
		}

		Child = child;
	}

	#endregion

	#region Properties

	public Size ClientSize { get; set; } = new(1000, 1000);
	public IInputElement CursorElement { get; set; }

	public IFocusManager FocusManager => _focusManager ??= new FocusManager { ContentRoot = this };
	public InputElement FocusRoot => this;
	public ITextInputMethodImpl InputMethod { get; }
	public IInputRoot InputRoot => this;

	public double LayoutScaling { get; set; } = 1;
	public IPlatformSettings PlatformSettings => PresentationLocator.Current.GetService<IPlatformSettings>();

	public IInputElement PointerOverElement { get; set; }
	public double RenderScaling => 1;
	public InputElement RootElement => this;

	public Visual RootVisual => this;

	public bool ShowAccessKeys { get; set; }

	public IStyleHost StylingParent { get; set; }
	internal IHitTester HitTester { get; set; }

	internal ILayoutManager LayoutManager { get; set; }

	internal IRenderer Renderer { get; set; }
	IHitTester IPresentationSource.HitTester => HitTester;

	ILayoutManager ILayoutRoot.LayoutManager => LayoutManager;

	ILayoutRoot IPresentationSource.LayoutRoot => this;
	IRenderer IPresentationSource.Renderer => Renderer;

	Layoutable ILayoutRoot.RootVisual => RootElement;

	IStyleHost IStyleHost.StylingParent => StylingParent;

	#endregion

	#region Methods

	public IRenderTarget CreateRenderTarget()
	{
		return new StubRenderTarget();
	}

	public void ExecuteInitialLayoutPass()
	{
		LayoutManager.ExecuteInitialLayoutPass();
	}

	public void Invalidate(Rect rect)
	{
	}

	public Point? PointToClient(PixelPoint p)
	{
		return p.ToPoint(1);
	}

	public PixelPoint? PointToScreen(Point p)
	{
		return PixelPoint.FromPoint(p, 1);
	}

	public void PointerOverInvalidated()
	{
	}

	public void RegisterChildrenNames()
	{
		var scope = NameScope.GetNameScope(this) ?? new NameScope();
		NameScope.SetNameScope(this, scope);

		void Visit(StyledElement element, bool force = false)
		{
			if (element.Name != null)
			{
				if (scope.Find(element.Name) != element)
				{
					scope.Register(element.Name, element);
				}
			}

			if (element is Visual visual && (force || (NameScope.GetNameScope(element) == null)))
			{
				foreach (var child in visual.GetVisualChildren())
				{
					if (child is StyledElement styledChild)
					{
						Visit(styledChild);
					}
				}
			}
		}

		Visit(this, true);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		return base.MeasureOverride(ClientSize);
	}

	#endregion

	#region Classes

	private class NullHitTester : IHitTester
	{
		#region Methods

		public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter)
		{
			return Array.Empty<Visual>();
		}

		public IEnumerable<GeometryHitTestResult> HitTest(Geometry geometry, Visual root, Func<Visual, bool> filter)
		{
			return Array.Empty<GeometryHitTestResult>();
		}

		public Visual HitTestFirst(Point p, Visual root, Func<Visual, bool> filter)
		{
			return null;
		}

		public GeometryHitTestResult HitTestFirst(Geometry geometry, Visual root, Func<Visual, bool> filter)
		{
			return null;
		}

		#endregion
	}

	#endregion
}