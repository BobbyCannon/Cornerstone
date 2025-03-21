#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Encapsulates and adds pointer hover support to controls.
/// </summary>
public class PointerHoverLogic : IDisposable
{
	#region Constants

	private const double PointerHoverHeight = 2;
	private const double PointerHoverWidth = 2;

	#endregion

	#region Fields

	private bool _disposed;
	private bool _hovering;
	private PointerEventArgs _hoverLastEventArgs;
	private Point _hoverStartPoint;

	private readonly Control _target;

	private DispatcherTimer _timer;
	private static readonly TimeSpan PointerHoverTime = TimeSpan.FromMilliseconds(400);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new instance and attaches itself to the <paramref name="target" /> UIElement.
	/// </summary>
	public PointerHoverLogic(Control target)
	{
		_target = target ?? throw new ArgumentNullException(nameof(target));
		_target.PointerExited += OnPointerLeave;
		_target.PointerMoved += OnPointerMoved;
		_target.PointerEntered += OnPointerEnter;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Removes the hover support from the target control.
	/// </summary>
	public void Dispose()
	{
		if (!_disposed)
		{
			_target.PointerExited -= OnPointerLeave;
			_target.PointerMoved -= OnPointerMoved;
			_target.PointerEntered -= OnPointerEnter;
		}
		_disposed = true;
	}

	/// <summary>
	/// Raises the <see cref="PointerHover" /> event.
	/// </summary>
	protected virtual void OnPointerHover(PointerEventArgs e)
	{
		PointerHover?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the <see cref="PointerHoverStopped" /> event.
	/// </summary>
	protected virtual void OnPointerHoverStopped(PointerEventArgs e)
	{
		PointerHoverStopped?.Invoke(this, e);
	}

	private void OnPointerEnter(object sender, PointerEventArgs e)
	{
		StartHovering(e);
		// do not set e.Handled - allow others to also handle the event
	}

	private void OnPointerLeave(object sender, PointerEventArgs e)
	{
		StopHovering();
		// do not set e.Handled - allow others to also handle the event
	}

	private void OnPointerMoved(object sender, PointerEventArgs e)
	{
		var movement = _hoverStartPoint - e.GetPosition(_target);
		if ((Math.Abs(movement.X) > PointerHoverWidth) ||
			(Math.Abs(movement.Y) > PointerHoverHeight))
		{
			StartHovering(e);
		}
		// do not set e.Handled - allow others to also handle the event
	}

	private void OnTimerElapsed(object sender, EventArgs e)
	{
		_timer.Stop();
		_timer = null;

		_hovering = true;
		OnPointerHover(_hoverLastEventArgs);
	}

	private void StartHovering(PointerEventArgs e)
	{
		StopHovering();
		_hoverStartPoint = e.GetPosition(_target);
		_hoverLastEventArgs = e;
		_timer = new DispatcherTimer(PointerHoverTime, DispatcherPriority.Background, OnTimerElapsed);
		_timer.Start();
	}

	private void StopHovering()
	{
		if (_timer != null)
		{
			_timer.Stop();
			_timer = null;
		}
		if (_hovering)
		{
			_hovering = false;
			OnPointerHoverStopped(_hoverLastEventArgs);
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Occurs when the pointer starts hovering over a certain location.
	/// </summary>
	public event EventHandler<PointerEventArgs> PointerHover;

	/// <summary>
	/// Occurs when the pointer stops hovering over a certain location.
	/// </summary>
	public event EventHandler<PointerEventArgs> PointerHoverStopped;

	#endregion
}