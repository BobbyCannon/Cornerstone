using System;

namespace Cornerstone.Presentation.Controls.Chrome
{
    public class NativeMenuItemBase : PresentationObject
    {
        private NativeMenu? _parent;

        internal NativeMenuItemBase()
        {

        }

        public static readonly DirectProperty<NativeMenuItemBase, NativeMenu?> ParentProperty =
            PresentationProperty.RegisterDirect<NativeMenuItemBase, NativeMenu?>(nameof(Parent), o => o.Parent);

        public NativeMenu? Parent
        {
            get => _parent;
            internal set => SetAndRaise(ParentProperty, ref _parent, value);
        }
    }
}
