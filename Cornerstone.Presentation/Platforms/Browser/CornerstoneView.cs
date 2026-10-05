using System;
using System.Runtime.InteropServices.JavaScript;
using Cornerstone.Presentation.Browser.Interop;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Embedding;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Browser
{
    public class CornerstoneView
    {
        private readonly EmbeddableControlRoot _topLevel;

        /// <param name="divId">ID of the html element where Cornerstone.Presentation content should be rendered.</param>
        public CornerstoneView(string divId)
            : this(DomHelper.GetElementById(divId, BrowserWindowingPlatform.GlobalThis) ??
                   throw new Exception($"Element with id '{divId}' was not found in the html document."))
        {
        }

        /// <param name="host">JSObject holding a div element where Cornerstone.Presentation content should be rendered.</param>
        public CornerstoneView(JSObject host)
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var hostContent = DomHelper.CreateCornerstoneHost(host);
            if (hostContent == null)
            {
                throw new InvalidOperationException("Cornerstone WASM host wasn't initialized.");
            }

            var nativeControlsContainer = hostContent.GetPropertyAsJSObject("nativeHost")
                                          ?? throw new InvalidOperationException("NativeHost cannot be null");
            var inputElement = hostContent.GetPropertyAsJSObject("inputElement")
                               ?? throw new InvalidOperationException("InputElement cannot be null");

            var topLevelImpl = new BrowserTopLevelImpl(host, nativeControlsContainer, inputElement);
            _topLevel = new EmbeddableControlRoot(topLevelImpl);

            _topLevel.Prepare();
            _topLevel.GotFocus += (_, _) => InputHelper.FocusElement(host);
            _topLevel.Renderer.Start(); // TODO: use Start+StopRenderer() instead.
            _topLevel.RequestAnimationFrame(_ =>
            {
                // Try to get local splash-screen of the specific host.
                // If couldn't find - get global one by ID for compatibility.
                var splash = DomHelper.GetElementsByClassName("cornerstone-splash", host)
                             ?? DomHelper.GetElementById("cornerstone-splash", BrowserWindowingPlatform.GlobalThis);
                if (splash is not null)
                {
                    DomHelper.AddCssClass(splash, "splash-close");
                    splash.Dispose();
                }
            });
        }

        public Control? Content
        {
            get => (Control)_topLevel.Content!;
            set => _topLevel.Content = value;
        }

        internal TopLevel TopLevel => _topLevel;
    }
}
