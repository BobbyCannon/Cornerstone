// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.ComponentModel;

namespace Cornerstone.Presentation.Controls.Input
{
    /// <summary>
    /// Provides data for the
    /// <see cref="E:Cornerstone.Presentation.Controls.AutoCompleteBox.Populating" />
    /// event.
    /// </summary>
    public class PopulatingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Gets the text that is used to determine which items to display in
        /// the <see cref="T:Cornerstone.Presentation.Controls.AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The text that is used to determine which items to display in
        /// the <see cref="T:Cornerstone.Presentation.Controls.AutoCompleteBox" />.</value>
        public string? Parameter { get; private set; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Cornerstone.Presentation.Controls.Input.PopulatingEventArgs" />.
        /// </summary>
        /// <param name="parameter">The value of the
        /// <see cref="P:Cornerstone.Presentation.Controls.AutoCompleteBox.SearchText" />
        /// property, which is used to filter items for the
        /// <see cref="T:Cornerstone.Presentation.Controls.AutoCompleteBox" /> control.</param>
        public PopulatingEventArgs(string? parameter)
        {
            Parameter = parameter;
        }
    }
}
