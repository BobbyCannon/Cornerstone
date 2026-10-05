#region References

using System;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Theme;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.RemoteLink.Android;

[SourceReflection]
public partial class AndroidTabView : UserControl<AndroidTabViewModel>
{
	#region Fields

	private AndroidTabViewModel _subscribed;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AndroidTabView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override AndroidTabViewModel CreateDesignData()
	{
		var viewModel = GetInstance<AndroidTabViewModel>();
		if ((viewModel == null) || (viewModel.Devices.Count > 0))
		{
			return viewModel;
		}

		viewModel.Devices.Add(new AndroidDeviceItem("emulator-5554", "device", "Pixel Preview"));
		viewModel.SelectedDevice = viewModel.Devices[0];
		viewModel.StatusText = "Design preview. Connect does not start a session.";
		return viewModel;
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		if (_subscribed != null)
		{
			_subscribed.PropertyChanged -= ViewModelOnPropertyChanged;
			_subscribed = null;
		}

		base.OnDataContextChanged(e);
		_subscribed = DataContext as AndroidTabViewModel;
		if (_subscribed != null)
		{
			_subscribed.PropertyChanged += ViewModelOnPropertyChanged;
			ApplyFrame(_subscribed);
		}
	}

	private void ApplyFrame(AndroidTabViewModel viewModel)
	{
		if ((PreviewSurface == null) || (viewModel == null))
		{
			return;
		}

		if (!viewModel.IsConnected
			|| (viewModel.PreviewPixels == null)
			|| (viewModel.PreviewWidth <= 0)
			|| (viewModel.PreviewHeight <= 0))
		{
			PreviewSurface.Clear();
			return;
		}

		PreviewSurface.SetFrame(viewModel.PreviewPixels, viewModel.PreviewWidth, viewModel.PreviewHeight);
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if ((e.PropertyName != nameof(AndroidTabViewModel.FrameVersion))
			&& (e.PropertyName != nameof(AndroidTabViewModel.PreviewPixels)))
		{
			return;
		}

		if (sender is AndroidTabViewModel viewModel)
		{
			ApplyFrame(viewModel);
		}
	}

	private void OnPreviewPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (ViewModel == null)
		{
			return;
		}

		if (!TryMapToNormalized(e, out var x, out var y))
		{
			return;
		}

		ViewModel.StorePointerStart(x, y);
		_ = ViewModel.SendPointerAsync(x, y, x, y, TimeSpan.Zero, false);
		e.Pointer.Capture(PreviewHost);
		PreviewHost.Focus();
		e.Handled = true;
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if ((ViewModel == null) || e.Handled || (e.Key == Key.None))
		{
			return;
		}

		if (!AndroidTabViewModel.TryMapSpecialKey(e.Key.ToString(), out var keyCode))
		{
			return;
		}

		_ = ViewModel.SendKeyboardKeyAsync(keyCode);
		e.Handled = true;
	}

	private void OnPreviewTextInput(object sender, TextInputEventArgs e)
	{
		if ((ViewModel == null) || e.Handled || string.IsNullOrEmpty(e.Text))
		{
			return;
		}

		_ = ViewModel.SendTextAsync(e.Text);
		e.Handled = true;
	}

	private void OnPreviewPointerMoved(object sender, PointerEventArgs e)
	{
		if ((ViewModel == null) || !ReferenceEquals(e.Pointer.Captured, PreviewHost))
		{
			return;
		}

		if (!TryMapToNormalized(e, out var x, out var y))
		{
			return;
		}

		_ = ViewModel.SendPointerMoveAsync(x, y);
		e.Handled = true;
	}

	private void OnPreviewPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		if (ViewModel == null)
		{
			return;
		}

		if (!TryMapToNormalized(e, out var x, out var y))
		{
			return;
		}

		var duration = DateTimeOffset.UtcNow - ViewModel.PointerStartTime;
		_ = ViewModel.SendPointerAsync(ViewModel.PointerStartX, ViewModel.PointerStartY, x, y, duration, true);
		e.Pointer.Capture(null);
		e.Handled = true;
	}

	private bool TryMapToNormalized(PointerEventArgs e, out double x, out double y)
	{
		x = 0;
		y = 0;
		if (PreviewSurface == null)
		{
			return false;
		}

		return PreviewSurface.TryMapToNormalized(e.GetPosition(PreviewSurface), out x, out y);
	}

	#endregion
}
