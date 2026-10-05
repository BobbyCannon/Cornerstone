using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class CsnAutomationPeer : NativeCallbackBase, ICsnAutomationPeer
    {
        private static readonly Dictionary<AutomationProperty, CsnAutomationProperty> s_propertyMap = new()
        {
            { AutomationElementIdentifiers.AutomationIdProperty, CsnAutomationProperty.AutomationPeer_AutomationId },
            { AutomationElementIdentifiers.BoundingRectangleProperty, CsnAutomationProperty.AutomationPeer_BoundingRectangle },
            { AutomationElementIdentifiers.ClassNameProperty, CsnAutomationProperty.AutomationPeer_ClassName },
            { AutomationElementIdentifiers.NameProperty, CsnAutomationProperty.AutomationPeer_Name },
            { RangeValuePatternIdentifiers.ValueProperty, CsnAutomationProperty.RangeValueProvider_Value },
            { ValuePatternIdentifiers.ValueProperty, CsnAutomationProperty.ValueProvider_Value },
            { TogglePatternIdentifiers.ToggleStateProperty, CsnAutomationProperty.ToggleProvider_ToggleState },
            { ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, CsnAutomationProperty.ExpandCollapseProvider_ExpandCollapseState },
            { SelectionItemPatternIdentifiers.IsSelectedProperty, CsnAutomationProperty.SelectionItemProvider_IsSelected },
            { SelectionPatternIdentifiers.SelectionProperty, CsnAutomationProperty.SelectionProvider_Selection },
        };

        private static readonly ConditionalWeakTable<AutomationPeer, CsnAutomationPeer> s_wrappers = new();
        private readonly AutomationPeer _inner;

        private CsnAutomationPeer(AutomationPeer inner)
        {
            _inner = inner;
            _inner.ChildrenChanged += (_, _) => Node?.ChildrenChanged();
            _inner.PropertyChanged += OnPeerPropertyChanged;
            if (inner is IRootProvider root)
                root.FocusChanged += (_, _) => Node?.FocusChanged(); 
        }

        ~CsnAutomationPeer() => Node?.Dispose();
        
        public ICsnAutomationNode? Node { get; private set; }
        public ICsnString? AcceleratorKey => _inner.GetAcceleratorKey().ToCsnString();
        public ICsnString? AccessKey => _inner.GetAccessKey().ToCsnString();
        public CsnAutomationControlType AutomationControlType => (CsnAutomationControlType)_inner.GetAutomationControlType();
        public ICsnString? AutomationId => _inner.GetAutomationId().ToCsnString();
        public CsnRect BoundingRectangle => _inner.GetBoundingRectangle().ToCsnRect();
        public ICsnAutomationPeerArray Children => new CsnAutomationPeerArray(_inner.GetChildren());
        public ICsnString? ClassName => _inner.GetClassName().ToCsnString();
        public ICsnAutomationPeer? LabeledBy => Wrap(_inner.GetLabeledBy());
        public ICsnString? Name => _inner.GetName().ToCsnString();
        public ICsnString? HelpText => _inner.GetHelpText().ToCsnString();
        public ICsnString? PlaceholderText => _inner.GetPlaceholderText().ToCsnString();
        public CsnLandmarkType LandmarkType => (CsnLandmarkType?)_inner.GetLandmarkType() ?? CsnLandmarkType.LandmarkNone;
        public int HeadingLevel => _inner.GetHeadingLevel();
        public ICsnAutomationPeer? Parent => Wrap(_inner.GetParent());
        public ICsnAutomationPeer? TemplatedParent =>
            _inner is ControlAutomationPeer { Owner.TemplatedParent: Control templatedParent } ? 
                Wrap(ControlAutomationPeer.CreatePeerForElement(templatedParent))
                : null;
        public ICsnAutomationPeer? VisualRoot => Wrap(_inner.GetAutomationRoot());
        public CsnLiveSetting LiveSetting => (CsnLiveSetting)_inner.GetLiveSetting();

        public int HasKeyboardFocus() => _inner.HasKeyboardFocus().AsComBool();
        public int IsContentElement() => _inner.IsContentElement().AsComBool();
        public int IsControlElement() => _inner.IsControlElement().AsComBool();
        public int IsEnabled() => _inner.IsEnabled().AsComBool();
        public int IsKeyboardFocusable() => _inner.IsKeyboardFocusable().AsComBool();
        public void SetFocus() => _inner.SetFocus();
        public int ShowContextMenu() => _inner.ShowContextMenu().AsComBool();
        public void BringIntoView() => _inner.BringIntoView();

        public void SetNode(ICsnAutomationNode node)
        {
            if (Node is not null)
                throw new InvalidOperationException("The CsnAutomationPeer already has a node.");
            Node = node;
        }

        public int IsInteropPeer() => (_inner is InteropAutomationPeer).AsComBool();
        public IntPtr InteropPeer_GetNativeControlHandle() => ((InteropAutomationPeer)_inner).NativeControlHandle.Handle;
        
        public ICsnAutomationPeer? RootPeer
        {
            get
            {
                var peer = _inner;
                var parent = peer.GetParent();

                while (peer.GetProvider<IRootProvider>() is null && parent is not null)
                {
                    peer = parent;
                    parent = peer.GetParent();
                }

                return Wrap(peer);
            }
        }

        private IEmbeddedRootProvider EmbeddedRootProvider => GetProvider<IEmbeddedRootProvider>();
        private IExpandCollapseProvider ExpandCollapseProvider => GetProvider<IExpandCollapseProvider>();
        private IInvokeProvider InvokeProvider => GetProvider<IInvokeProvider>();
        private IRangeValueProvider RangeValueProvider => GetProvider<IRangeValueProvider>();
        private IRootProvider RootProvider => GetProvider<IRootProvider>();
        private ISelectionItemProvider SelectionItemProvider => GetProvider<ISelectionItemProvider>();
        private IToggleProvider ToggleProvider => GetProvider<IToggleProvider>();
        private IValueProvider ValueProvider => GetProvider<IValueProvider>();

        public int IsRootProvider() => IsProvider<IRootProvider>();

        public ICsnWindowBase? RootProvider_GetWindow() => (RootProvider.PlatformImpl as WindowBaseImpl)?.Native;
        public ICsnAutomationPeer? RootProvider_GetFocus() => Wrap(RootProvider.GetFocus());

        public ICsnAutomationPeer? RootProvider_GetPeerFromPoint(CsnPoint point)
        {
            var result = RootProvider.GetPeerFromPoint(point.ToPoint());

            if (result is null)
                return null;

            // The OSX accessibility APIs expect non-ignored elements when hit-testing.
            while (!result.IsControlElement())
            {
                var parent = result.GetParent();

                if (parent is not null)
                    result = parent;
                else
                    break;
            }
            
            return Wrap(result);
        }


        public int IsEmbeddedRootProvider() => IsProvider<IEmbeddedRootProvider>();

        public ICsnAutomationPeer? EmbeddedRootProvider_GetFocus() => Wrap(EmbeddedRootProvider.GetFocus());

        public ICsnAutomationPeer? EmbeddedRootProvider_GetPeerFromPoint(CsnPoint point)
        {
            var result = EmbeddedRootProvider.GetPeerFromPoint(point.ToPoint());

            if (result is null)
                return null;

            // The OSX accessibility APIs expect non-ignored elements when hit-testing.
            while (!result.IsControlElement())
            {
                var parent = result.GetParent();

                if (parent is not null)
                    result = parent;
                else
                    break;
            }

            return Wrap(result);
        }

        public int IsExpandCollapseProvider() => IsProvider<IExpandCollapseProvider>();

        public int ExpandCollapseProvider_GetIsExpanded() => ExpandCollapseProvider.ExpandCollapseState switch
        {
            ExpandCollapseState.Expanded => 1,
            ExpandCollapseState.PartiallyExpanded => 1,
            _ => 0,
        };

        public int ExpandCollapseProvider_GetShowsMenu() => ExpandCollapseProvider.ShowsMenu.AsComBool();
        public void ExpandCollapseProvider_Expand() => ExpandCollapseProvider.Expand();
        public void ExpandCollapseProvider_Collapse() => ExpandCollapseProvider.Collapse();

        public int IsInvokeProvider() => IsProvider<IInvokeProvider>();
        public void InvokeProvider_Invoke() => InvokeProvider.Invoke();

        public int IsRangeValueProvider() => IsProvider<IRangeValueProvider>();
        public double RangeValueProvider_GetValue() => RangeValueProvider.Value;
        public double RangeValueProvider_GetMinimum() => RangeValueProvider.Minimum;
        public double RangeValueProvider_GetMaximum() => RangeValueProvider.Maximum;
        public double RangeValueProvider_GetSmallChange() => RangeValueProvider.SmallChange;
        public double RangeValueProvider_GetLargeChange() => RangeValueProvider.LargeChange;
        public void RangeValueProvider_SetValue(double value) => RangeValueProvider.SetValue(value);
        public int RangeValueProvider_IsReadOnly() => RangeValueProvider.IsReadOnly.AsComBool();

        public int IsSelectionItemProvider() => IsProvider<ISelectionItemProvider>();
        public int SelectionItemProvider_IsSelected() => SelectionItemProvider.IsSelected.AsComBool();
        public void SelectionItemProvider_Select() => SelectionItemProvider.Select();
        public void SelectionItemProvider_AddToSelection() => SelectionItemProvider.AddToSelection();
        public void SelectionItemProvider_RemoveFromSelection() => SelectionItemProvider.RemoveFromSelection();

        public ICsnAutomationPeer? ScrollProvider_GetHorizontalScrollBar()
            => _inner is ScrollViewerAutomationPeer scrollViewer ? Wrap(scrollViewer.GetHorizontalScrollBarPeer()) : null;

        public ICsnAutomationPeer? ScrollProvider_GetVerticalScrollBar()
            => _inner is ScrollViewerAutomationPeer scrollViewer ? Wrap(scrollViewer.GetVerticalScrollBarPeer()) : null;
        
        public int IsToggleProvider() => IsProvider<IToggleProvider>();
        public int ToggleProvider_GetToggleState() => (int)ToggleProvider.ToggleState;
        public void ToggleProvider_Toggle() => ToggleProvider.Toggle();

        public int IsValueProvider() => IsProvider<IValueProvider>();
        public ICsnString? ValueProvider_GetValue() => ValueProvider.Value.ToCsnString();
        public void ValueProvider_SetValue(string value) => ValueProvider.SetValue(value);
        public int ValueProvider_IsReadOnly() => ValueProvider.IsReadOnly.AsComBool();

        [return: NotNullIfNotNull("peer")]
        public static CsnAutomationPeer? Wrap(AutomationPeer? peer)
        {
            return peer is null ? null : s_wrappers.GetValue(peer, x => new(peer));
        }

        private T GetProvider<T>()
        {
            return _inner.GetProvider<T>() ?? throw new InvalidOperationException(
                $"The peer {_inner} does not implement {typeof(T)}.");
        }

        private int IsProvider<T>() => (_inner.GetProvider<T>() is not null).AsComBool();

        private void OnPeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e)
        {
            if (s_propertyMap.TryGetValue(e.Property, out var property))
                Node?.PropertyChanged(property);
        }
    }

    internal class CsnAutomationPeerArray : NativeCallbackBase, ICsnAutomationPeerArray
    {
        private readonly CsnAutomationPeer[] _items;
        
        public CsnAutomationPeerArray(IReadOnlyList<AutomationPeer> items)
        {
            _items = items.Select(x => CsnAutomationPeer.Wrap(x)).ToArray();
        }
        
        public uint Count => (uint)_items.Length;
        public ICsnAutomationPeer Get(uint index) => _items[index];
    }
}
