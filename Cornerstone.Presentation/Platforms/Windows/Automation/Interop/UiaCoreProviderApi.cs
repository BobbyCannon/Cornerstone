using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Cornerstone.Presentation.Platforms.Windows.Automation.Interop
{
    [Guid("d8e55844-7043-4edc-979d-593cc6b4775e")]
    internal enum AsyncContentLoadedState
    {
        Beginning,
        Progress,
        Completed,
    }

    [Guid("e4cfef41-071d-472c-a65c-c14f59ea81eb")]
    internal enum StructureChangeType
    {
        ChildAdded,
        ChildRemoved,
        ChildrenInvalidated,
        ChildrenBulkAdded,
        ChildrenBulkRemoved,
        ChildrenReordered,
    }

    internal enum UiaEventId
    {
        ToolTipOpened = 20000,
        ToolTipClosed,
        StructureChanged,
        MenuOpened,
        AutomationPropertyChanged,
        AutomationFocusChanged,
        AsyncContentLoaded,
        MenuClosed,
        LayoutInvalidated,
        Invoke_Invoked,
        SelectionItem_ElementAddedToSelection,
        SelectionItem_ElementRemovedFromSelection,
        SelectionItem_ElementSelected,
        Selection_Invalidated,
        Text_TextSelectionChanged,
        Text_TextChanged,
        Window_WindowOpened,
        Window_WindowClosed,
        MenuModeStart,
        MenuModeEnd,
        InputReachedTarget,
        InputReachedOtherElement,
        InputDiscarded,
        SystemAlert,
        LiveRegionChanged,
        HostedFragmentRootsInvalidated,
        Drag_DragStart,
        Drag_DragCancel,
        Drag_DragComplete,
        DropTarget_DragEnter,
        DropTarget_DragLeave,
        DropTarget_Dropped,
        TextEdit_TextChanged,
        TextEdit_ConversionTargetChanged,
        Changes
    };

    internal static partial class UiaCoreProviderApi
    {
        public const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);

        private static readonly StrategyBasedComWrappers s_comWrappers = new();
        private static readonly Guid s_iidRawElementProviderSimple = new("d6dd68d1-86fd-4332-8666-9abedea2d24c");

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UiaClientsAreListening();

        public static IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el)
        {
            var provider = AsNativeProvider(el);
            try
            {
                return UiaReturnRawElementProviderNative(hwnd, wParam, lParam, provider);
            }
            finally
            {
                ReleaseNative(provider);
            }
        }

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaHostProviderFromHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple provider);

        public static int UiaRaiseAutomationEvent(IRawElementProviderSimple provider, int id)
        {
            var native = AsNativeProvider(provider);
            try
            {
                return UiaRaiseAutomationEventNative(native, id);
            }
            finally
            {
                ReleaseNative(native);
            }
        }

        public static int UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int id, object oldValue, object newValue)
        {
            var native = AsNativeProvider(provider);
            try
            {
                return UiaRaiseAutomationPropertyChangedEventNative(native, id, oldValue, newValue);
            }
            finally
            {
                ReleaseNative(native);
            }
        }

        public static int UiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId, int runtimeIdLen)
        {
            var native = AsNativeProvider(provider);
            try
            {
                return UiaRaiseStructureChangedEventNative(native, structureChangeType, runtimeId, runtimeIdLen);
            }
            finally
            {
                ReleaseNative(native);
            }
        }

        public static int UiaDisconnectProvider(IRawElementProviderSimple provider)
        {
            var native = AsNativeProvider(provider);
            try
            {
                return UiaDisconnectProviderNative(native);
            }
            finally
            {
                ReleaseNative(native);
            }
        }

        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaReturnRawElementProvider", StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr UiaReturnRawElementProviderNative(IntPtr hwnd, IntPtr wParam, IntPtr lParam, nint el);

        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseAutomationEvent", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int UiaRaiseAutomationEventNative(nint provider, int id);

        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseAutomationPropertyChangedEvent", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int UiaRaiseAutomationPropertyChangedEventNative(nint provider, int id, [MarshalUsing(typeof(ComVariantMarshaller))] object oldValue, [MarshalUsing(typeof(ComVariantMarshaller))] object newValue);

        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseStructureChangedEvent", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int UiaRaiseStructureChangedEventNative(nint provider, StructureChangeType structureChangeType, int[] runtimeId, int runtimeIdLen);

        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaDisconnectProvider", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int UiaDisconnectProviderNative(nint provider);

        private static nint AsNativeProvider(IRawElementProviderSimple el)
        {
            if (el is null)
            {
                return 0;
            }

            var unknown = s_comWrappers.GetOrCreateComInterfaceForObject(el, CreateComInterfaceFlags.None);
            if (unknown == 0)
            {
                return 0;
            }

            var iid = s_iidRawElementProviderSimple;
            var hr = Marshal.QueryInterface(unknown, in iid, out var provider);
            Marshal.Release(unknown);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return provider;
        }

        private static void ReleaseNative(nint provider)
        {
            if (provider != 0)
            {
                Marshal.Release(provider);
            }
        }
    }
}
