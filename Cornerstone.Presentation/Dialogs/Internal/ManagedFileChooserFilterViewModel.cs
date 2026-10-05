using System.Linq;
using System.Text.RegularExpressions;
using Cornerstone.Presentation.Platform.Storage;

namespace Cornerstone.Presentation.Dialogs.Internal
{
    public class ManagedFileChooserFilterViewModel : PresentationDialogsInternalViewModelBase
    {
        private readonly Regex[]? _patterns;
        public string Name { get; }
        internal int Index { get; }

        public ManagedFileChooserFilterViewModel(FilePickerFileType filter) : this(filter, 0)
        {
        }
        
        public ManagedFileChooserFilterViewModel(FilePickerFileType filter, int index)
        {
            Index = index;
            Name = filter.Name;

            if (filter.Patterns?.Contains("*.*") == true)
            {
                return;
            }

             _patterns = filter.Patterns?
                           .Select(e => new Regex("^" + Regex.Escape(e).Replace("\\*", ".*") + "$", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                           .ToArray();
        }

        public bool Match(string filename)
        {
            return _patterns == null || _patterns.Any(ext => ext.IsMatch(filename));
        }

        public override string ToString() => Name;
    }
}
