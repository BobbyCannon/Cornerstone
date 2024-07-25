#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;
using Cornerstone.Avalonia.AvaloniaWebView.Helpers;
using Cornerstone.Avalonia.AvaloniaWebView.Shared;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

public sealed partial class WebView : CornerstoneControl, IVirtualWebView<WebView>, IEmptyView, IWebViewEventHandler, IVirtualWebViewControlCallBack, IWebViewControl
{
	#region Fields

	private readonly BorderRenderHelper _borderRenderHelper = new();

	private readonly WebViewCreationProperties _creationProperties;
	private Thickness? _layoutThickness;
	private readonly ContentPresenter _partEmptyViewPresenter;

	private readonly Border _partInnerContainer;

	private double _scale;
	private readonly IViewHandlerProvider _viewHandlerProvider;

	#endregion

	#region Constructors

	public WebView()
		: this(default)
	{
	}

	public WebView(IServiceProvider serviceProvider = default)
	{
		//var properties = WebViewLocator.s_ResolverContext.GetRequiredService<WebViewCreationProperties>();
		var properties = CornerstoneApplication.GetService<WebViewCreationProperties>();
		_creationProperties = properties ?? new WebViewCreationProperties();

		//_viewHandlerProvider = WebViewLocator.s_ResolverContext.GetRequiredService<IViewHandlerProvider>();
		_viewHandlerProvider = CornerstoneApplication.GetService<IViewHandlerProvider>();
		ClipToBounds = false;

		_partEmptyViewPresenter = new()
		{
			[!ContentPresenter.ContentProperty] = this[!EmptyViewerProperty],
			[!ContentPresenter.ContentTemplateProperty] = this[!EmptyViewerTemplateProperty]
		};

		_partInnerContainer = new()
		{
			Child = _partEmptyViewPresenter,
			ClipToBounds = true,
			[!Border.CornerRadiusProperty] = this[!CornerRadiusProperty]
		};
		Child = _partInnerContainer;
	}

	static WebView()
	{
		AffectsRender<WebView>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, BoxShadowProperty);
		AffectsMeasure<WebView>(ChildProperty, PaddingProperty, BorderThicknessProperty);
		LoadDependencyObjectsChanged();
		LoadHostDependencyObjectsChanged();
	}

	#endregion

	#region Properties

	public IPlatformWebView PlatformWebView { get; private set; }

	#endregion
}