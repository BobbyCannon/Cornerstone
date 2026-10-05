#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Rendering;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

internal sealed class NullRenderer : IRenderer
{
	#region Constructors

	public NullRenderer()
	{
		Calls = new StubCallLog();
		Diagnostics = new RendererDiagnostics();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public RendererDiagnostics Diagnostics { get; }

	#endregion

	#region Methods

	public void AddDirty(Visual visual)
	{
		Calls.Add(nameof(AddDirty), visual);
	}

	public void Dispose()
	{
		Calls.Add(nameof(Dispose));
	}

	public IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter)
	{
		return Enumerable.Empty<Visual>();
	}

	public Visual HitTestFirst(Point p, Visual root, Func<Visual, bool> filter)
	{
		return null;
	}

	public void Paint(Rect rect)
	{
	}

	public void RecalculateChildren(Visual visual)
	{
		Calls.Add(nameof(RecalculateChildren), visual);
	}

	public void Resized(Size size)
	{
	}

	public void Start()
	{
	}

	public void Stop()
	{
	}

	public ValueTask<object> TryGetRenderInterfaceFeature(Type featureType)
	{
		return new((object) null);
	}

	#endregion

	#region Events

	event EventHandler<SceneInvalidatedEventArgs> IRenderer.SceneInvalidated
	{
		add { }
		remove { }
	}

	#endregion
}