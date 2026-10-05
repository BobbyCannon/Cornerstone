using System;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls
{
    [TemplatePart("PART_NativeMenuPresenter", typeof(MenuBase))]
    public class NativeMenuBar : TemplatedControl
    {
        private MenuBase? _menu;
        private IDisposable? _subscriptions;

        static NativeMenuBar()
        {
            // TODO12 Ideally we should make NativeMenuBar inherit MenuBase directly, but it would be a breaking change for 11.x.
            // Changing default template while keeping old StyleKeyOverride => Menu isn't a breaking change.
            TemplateProperty.OverrideDefaultValue<NativeMenuBar>(new FuncControlTemplate((_, ns) => new NativeMenuBarPresenter
            {
                Name = "PART_NativeMenuPresenter",
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty)
            }.RegisterInNameScope(ns)));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _menu = e.NameScope.Find<MenuBase>("PART_NativeMenuPresenter")
                    ?? this.FindDescendantOfType<MenuBase>()
                    ?? throw new InvalidOperationException("NativeMenuBar requires a MenuBase#PART_NativeMenuPresenter template part.");
            
            if (TopLevel.GetTopLevel(this) is {} topLevel)
            {
                SubscribeToToplevel(topLevel, _menu);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_menu is null)
                return;

            if (TopLevel.GetTopLevel(this) is {} topLevel)
            {
                SubscribeToToplevel(topLevel, _menu);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        private void SubscribeToToplevel(TopLevel topLevel, MenuBase menu)
        {
            _subscriptions?.Dispose();
            _subscriptions = new CompositeDisposable(
                menu.Bind(IsVisibleProperty, topLevel.GetBindingObservable(NativeMenu.IsNativeMenuExportedProperty)
                    .Select(v => !v.GetValueOrDefault<bool>())),
                menu.Bind(ItemsControl.ItemsSourceProperty, topLevel.GetBindingObservable(NativeMenu.MenuProperty)
                    .Select(v => v.GetValueOrDefault<NativeMenu>()?.Items)));
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new NativeMenuBarAutomationPeer(this);
    }
}
