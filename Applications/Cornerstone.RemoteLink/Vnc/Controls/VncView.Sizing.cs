using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Threading;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.MessageTypes.Outgoing;
using Size = Cornerstone.RemoteLink.Vnc.Client.Size;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.RemoteLink.Vnc.Controls;

public partial class VncView
{
	private enum SizeSource
	{
		None,
		OwnBounds,
		OptimalSizeProperty
	}

	private static readonly TimeSpan ThrottleTime = TimeSpan.FromSeconds(0.5);

	public static readonly DirectProperty<VncView, bool> AutoResizeRemoteProperty =
		PresentationProperty.RegisterDirect<VncView, bool>(nameof(AutoResizeRemote), o => o.AutoResizeRemote, (o, v) => o.AutoResizeRemote = v, true);

	public static readonly DirectProperty<VncView, global::Cornerstone.Presentation.Size?> OptimalSizeProperty =
		PresentationProperty.RegisterDirect<VncView, global::Cornerstone.Presentation.Size?>(nameof(OptimalSize), o => o.OptimalSize, (o, v) => o.OptimalSize = v);

	private bool _autoResizeRemote = true;
	private global::Cornerstone.Presentation.Size? _optimalSize;
	private SizeSource _sizeSource = SizeSource.None;
	private DispatcherTimer _resizeTimer;
	private global::Cornerstone.Presentation.Size _pendingSize;

	public bool AutoResizeRemote
	{
		get => _autoResizeRemote;
		set
		{
			if (value)
			{
				SetSizeSource(OptimalSize == null ? SizeSource.OwnBounds : SizeSource.OptimalSizeProperty);
			}
			else
			{
				SetSizeSource(SizeSource.None);
			}

			SetAndRaise(AutoResizeRemoteProperty, ref _autoResizeRemote, value);
		}
	}

	public global::Cornerstone.Presentation.Size? OptimalSize
	{
		get => _optimalSize;
		set
		{
			if (AutoResizeRemote)
			{
				SetSizeSource(value == null ? SizeSource.OwnBounds : SizeSource.OptimalSizeProperty);
			}
			else
			{
				SetSizeSource(SizeSource.None);
			}

			SetAndRaise(OptimalSizeProperty, ref _optimalSize, value);
		}
	}

	private void InitSizing()
	{
		_resizeTimer = new DispatcherTimer { Interval = ThrottleTime };
		_resizeTimer.Tick += ResizeTimerOnTick;
		SetSizeSource(SizeSource.OwnBounds);
	}

	private void SendInitialSizeUpdate()
	{
		var connection = Connection;
		if (connection == null)
		{
			return;
		}

		if (connection.DesktopIsResizable)
		{
			SendManualSizeUpdate();
			return;
		}

		void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(connection.DesktopIsResizable))
			{
				return;
			}

			if (!connection.DesktopIsResizable)
			{
				return;
			}

			connection.PropertyChanged -= PropertyChangedHandler;
			SendManualSizeUpdate();
		}

		connection.PropertyChanged += PropertyChangedHandler;
		AddConnectionDetach(new ActionDisposable(() => connection.PropertyChanged -= PropertyChangedHandler));
	}

	private void SendManualSizeUpdate()
	{
		global::Cornerstone.Presentation.Size size;
		switch (_sizeSource)
		{
			case SizeSource.OwnBounds:
				size = Bounds.Size;
				break;
			case SizeSource.OptimalSizeProperty:
				if (!OptimalSize.HasValue)
				{
					return;
				}

				size = OptimalSize.Value;
				break;
			default:
				return;
		}

		SendSetDesktopSize(size);
	}

	private void SetSizeSource(SizeSource newSizeSource)
	{
		if (newSizeSource == _sizeSource)
		{
			return;
		}

		_sizeSource = newSizeSource;
		SizeChanged -= OnViewSizeChanged;
		_resizeTimer?.Stop();

		if (_sizeSource == SizeSource.None)
		{
			return;
		}

		SizeChanged += OnViewSizeChanged;
	}

	private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
	{
		ScheduleSize(e.NewSize);
	}

	private void ScheduleSize(global::Cornerstone.Presentation.Size size)
	{
		_pendingSize = size;
		if (_resizeTimer == null)
		{
			return;
		}

		_resizeTimer.Stop();
		_resizeTimer.Start();
	}

	private void ResizeTimerOnTick(object sender, EventArgs e)
	{
		_resizeTimer.Stop();
		SendSetDesktopSize(_pendingSize);
	}

	private void SendSetDesktopSize(global::Cornerstone.Presentation.Size size)
	{
		var connection = Connection;
		if (connection == null)
		{
			return;
		}

		if (!connection.DesktopIsResizable)
		{
			return;
		}

		connection.EnqueueMessage(new SetDesktopSizeMessage((currentSize, currentLayout) =>
		{
			var newSize = new Size((int)size.Width, (int)size.Height);
			var newRectangle = new Cornerstone.RemoteLink.Vnc.Client.Rectangle(Position.Origin, newSize);

			Screen newScreen;
			if (!currentLayout.Any())
			{
				newScreen = new Screen(1, newRectangle, 0);
			}
			else
			{
				var firstScreen = currentLayout.First();
				newScreen = new Screen(firstScreen.Id, newRectangle, firstScreen.Flags);
			}

			return (newSize, new[] { newScreen }.ToImmutableHashSet());
		}));
	}

	private sealed class ActionDisposable : IDisposable
	{
		private readonly Action _action;

		public ActionDisposable(Action action)
		{
			_action = action;
		}

		public void Dispose()
		{
			_action();
		}
	}
}
