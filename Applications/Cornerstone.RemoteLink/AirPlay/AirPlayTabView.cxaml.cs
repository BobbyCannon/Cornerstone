#region References

using System;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Theme;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

[SourceReflection]
public partial class AirPlayTabView : UserControl<AirPlayTabViewModel>
{
	#region Fields

	private AirPlayTabViewModel _subscribed;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AirPlayTabView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnDataContextChanged(EventArgs e)
	{
		if (_subscribed != null)
		{
			_subscribed.PropertyChanged -= ViewModelOnPropertyChanged;
			_subscribed = null;
		}

		base.OnDataContextChanged(e);

		_subscribed = DataContext as AirPlayTabViewModel;
		if (_subscribed != null)
		{
			_subscribed.PropertyChanged += ViewModelOnPropertyChanged;
			ApplyFrame(_subscribed);
		}
	}

	private void ApplyFrame(AirPlayTabViewModel viewModel)
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
		if ((e.PropertyName != nameof(AirPlayTabViewModel.FrameVersion))
			&& (e.PropertyName != nameof(AirPlayTabViewModel.PreviewPixels)))
		{
			return;
		}

		if (sender is AirPlayTabViewModel viewModel)
		{
			ApplyFrame(viewModel);
		}
	}

	#endregion
}
