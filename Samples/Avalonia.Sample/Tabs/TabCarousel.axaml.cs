#region References

using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabCarousel : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Carousel";

	#endregion

	#region Fields

	private readonly Button _buttonNext;
	private readonly Button _buttonPrevious;
	private readonly Carousel _carousel;
	private readonly ComboBox _orientation;
	private readonly ComboBox _transition;

	#endregion

	#region Constructors

	// Constructor.

	public TabCarousel()
	{
		InitializeComponent();
		_carousel = this.Get<Carousel>("Carousel");
		_buttonPrevious = this.Get<Button>("ButtonPrevious");
		_buttonNext = this.Get<Button>("ButtonNext");
		_transition = this.Get<ComboBox>("Transition");
		_orientation = this.Get<ComboBox>("Orientation");
		_buttonPrevious.Click += (_, _) => _carousel.Previous();
		_buttonNext.Click += (_, _) => _carousel.Next();
		_transition.SelectionChanged += TransitionChanged;
		_orientation.SelectionChanged += TransitionChanged;
	}

	#endregion

	#region Methods

	// Transition changed event handler.

	private void TransitionChanged(object sender, SelectionChangedEventArgs e)
	{
		_carousel.PageTransition = _transition.SelectedIndex switch
		{
			0 => null,
			1 => new PageSlide(TimeSpan.FromSeconds(0.25), _orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical),
			2 => new CrossFade(TimeSpan.FromSeconds(0.25)),
			3 => new Rotate3DTransition(TimeSpan.FromSeconds(0.5), _orientation.SelectedIndex == 0 ? PageSlide.SlideAxis.Horizontal : PageSlide.SlideAxis.Vertical),
			_ => _carousel.PageTransition
		};
	}

	#endregion
}