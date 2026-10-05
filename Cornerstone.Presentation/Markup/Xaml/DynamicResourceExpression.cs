using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml.Converters;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Resources;

namespace Cornerstone.Presentation.Markup.Xaml.MarkupExtensions
{
    internal class DynamicResourceExpression : UntypedBindingExpressionBase
    {
        private readonly object _resourceKey;
        private readonly object? _anchor;
        private IResourceHost? _host;
        private IResourceProvider? _provider;
        private bool _overrideThemeVariant;
        private bool _targetTypeIsBrush;
        private ThemeVariant? _themeVariant;

        public DynamicResourceExpression(
            object resourceKey,
            object? anchor,
            ThemeVariant? themeVariant,
            BindingPriority priority)
            : base(priority)
        {
            _resourceKey = resourceKey;
            _anchor = anchor;
            _themeVariant = themeVariant;
        }

        public override string Description => $"DynamicResource {_resourceKey}";

        protected override void StartCore()
        {
            if (!TryGetResourceHost(out _host))
            {
                // The target is not an IResourceHost, so we need to find one from the anchor.
                if (_anchor is IResourceProvider provider)
                {
                    _provider = provider;
                    _host = provider.Owner;
                    _overrideThemeVariant = _themeVariant is not null;
                }
            }

            // If we wouldn't find a host or provider then log an error: we can't do anything.
            if (_host is null && _provider is null)
            {
                Log("Unable to find IResourceHost or IResourceProvider from which to lookup " +
                    $"DynamicResource {_resourceKey}.", LogEventLevel.Error);
                return;
            }

            // Hook up events.
            if (_provider is not null)
                _provider.OwnerChanged += OnResourceProviderOwnerChanged;
            Subscribe(_host);

            // And publish the initial value.
            _targetTypeIsBrush = TargetType == typeof(IBrush);
            PublishValue();
        }

        protected override void StopCore()
        {
            if (_provider is not null)
                _provider.OwnerChanged -= OnResourceProviderOwnerChanged;
            Unsubscribe(_host);
            _host = null;
            _provider = null;
        }

        private void OnResourceProviderOwnerChanged(object? sender, EventArgs e)
        {
            Unsubscribe(_host);
            _host = _provider?.Owner;
            Subscribe(_host);
            PublishValue();
        }

        private void ResourcesChanged(object? sender, ResourcesChangedEventArgs e) => PublishValue();

        private void ActualThemeVariantChanged(object? sender, EventArgs e)
        {
            if (!IsRunning)
                return;

            _themeVariant = ((IThemeVariantHost)sender!).ActualThemeVariant;
            PublishValue();
        }

        private void PublishValue()
        {
            if (_host is not null)
            {
                var theme = _themeVariant;
                var value = _host.FindResource(theme, _resourceKey) ?? PresentationProperty.UnsetValue;
                var convertedValue = _targetTypeIsBrush ?
                    ColorToBrushConverter.Convert(value, typeof(IBrush)) :
                    value;
                PublishValue(convertedValue);
            }
            else
            {
                PublishValue(PresentationProperty.UnsetValue);
            }
        }

        private bool TryGetResourceHost([NotNullWhen(true)] out IResourceHost? host)
        {
            if (TryGetTarget(out var target) && target is IResourceHost targetHost)
            {
                host = targetHost;
                return true;
            }

            if (_anchor is IResourceHost anchorHost)
            {
                host = anchorHost;
                return host is not null;
            }

            host = null;
            return false;
        }

        private void Subscribe(IResourceHost? host)
        {
            if (host is not null)
            {
                host.ResourcesChanged += ResourcesChanged;

                if (!_overrideThemeVariant && _host is IThemeVariantHost themeVariantHost)
                {
                    _themeVariant = themeVariantHost.ActualThemeVariant;
                    themeVariantHost.ActualThemeVariantChanged += ActualThemeVariantChanged;
                }
            }
        }

        private void Unsubscribe(IResourceHost? host)
        {
            if (host is not null)
            {
                host.ResourcesChanged -= ResourcesChanged;

                if (!_overrideThemeVariant && _host is IThemeVariantHost themeVariantHost)
                    themeVariantHost.ActualThemeVariantChanged -= ActualThemeVariantChanged;
            }
        }
    }
}
