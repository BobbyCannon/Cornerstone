using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Dialogs
{
    public record ManagedFileDialogOptions
    {
        public bool AllowDirectorySelection { get; set; }

        /// <summary>
        /// Allows to redefine how root volumes are populated in the dialog. 
        /// </summary>
        public IMountedVolumeInfoProvider? CustomVolumeInfoProvider { get; set; }

        /// <summary>
        /// Allows to redefine content root.
        /// Can be a custom Window or any ContentControl (Popup hosted).   
        /// </summary>
        public Func<ContentControl>? ContentRootFactory { get; set; } 
    }
}
