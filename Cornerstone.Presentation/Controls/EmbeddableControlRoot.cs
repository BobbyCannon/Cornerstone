using System;
using System.ComponentModel;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Automation;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls
{
    public class EmbeddableControlRoot : TopLevel, IFocusScope, IDisposable
    {
        public EmbeddableControlRoot(ITopLevelImpl impl) : base(impl)
        {
        }

        public EmbeddableControlRoot() : base(PlatformManager.CreateEmbeddableTopLevel())
        {
        }

        protected bool EnforceClientSize { get; set; } = true;

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.ExecuteInitialLayoutPass();
        }

        public new void StartRendering() => base.StartRendering();

        public new void StopRendering() => base.StopRendering();
        
        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            if (EnforceClientSize)
                availableSize = PlatformImpl?.ClientSize ?? default(Size);
            var rv = base.MeasureOverride(availableSize);
            if (EnforceClientSize)
                return availableSize;
            return rv;
        }

        protected override Type StyleKeyOverride => typeof(EmbeddableControlRoot);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            EnableVisualLayerManagerLayers();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EmbeddableControlRootAutomationPeer(this);
        }

        public void Dispose()
        {
            PlatformImpl?.Dispose();
            EnsureClosed();
        }
    }
}
