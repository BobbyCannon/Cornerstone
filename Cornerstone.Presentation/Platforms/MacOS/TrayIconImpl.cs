using System;
using System.IO;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class TrayIconImpl : ITrayIconWithIsTemplateImpl
    {
        private readonly ICsnTrayIcon _native;

        public TrayIconImpl(ICornerstoneNativeFactory factory)
        {
            _native = factory.CreateTrayIcon();

            MenuExporter = new MacMenuExporter(_native, factory);
        }

        public Action? OnClicked { get; set; }

        public void Dispose()
        {
            _native.Dispose();
        }

        public unsafe void SetIcon(IWindowIconImpl? icon)
        {
            if (icon is null)
            {
                _native.SetIcon(null, IntPtr.Zero);
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    icon.Save(ms);

                    var imageData = ms.ToArray();

                    fixed (void* ptr = imageData)
                    {
                        _native.SetIcon(ptr, new IntPtr(imageData.Length));
                    }
                }
            }
        }

        public void SetToolTipText(string? text)
        {
            _native.SetToolTipText(text);
        }

        public void SetIsVisible(bool visible)
        {
            _native.SetIsVisible(visible.AsComBool());
        }

        public void SetIsTemplateIcon(bool isTemplateIcon)
        {
            _native.SetIsTemplateIcon(isTemplateIcon.AsComBool());
        }

        public INativeMenuExporter? MenuExporter { get; }
    }
}
