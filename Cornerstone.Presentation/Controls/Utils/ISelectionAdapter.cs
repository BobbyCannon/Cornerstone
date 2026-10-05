// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls.Utils
{
    /// <summary>
    /// Defines an item collection, selection members, and key handling for the
    /// selection adapter contained in the drop-down portion of an
    /// <see cref="T:Cornerstone.Presentation.Controls.AutoCompleteBox" /> control.
    /// </summary>
    public interface ISelectionAdapter
    {
        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <value>The currently selected item.</value>
        object? SelectedItem { get; set; }

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Cornerstone.Presentation.Controls.Utils.ISelectionAdapter.SelectedItem" />
        /// property value changes.
        /// </summary>
        event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        
        /// <summary>
        /// Gets or sets a collection that is used to generate content for the
        /// selection adapter.
        /// </summary>
        /// <value>The collection that is used to generate content for the
        /// selection adapter.</value>
        IEnumerable? ItemsSource { get; set; }

        /// <summary>
        /// Occurs when a selected item is not cancelled and is committed as the
        /// selected item.
        /// </summary>
        event EventHandler<RoutedEventArgs>? Commit;

        /// <summary>
        /// Occurs when a selection has been cancelled.
        /// </summary>
        event EventHandler<RoutedEventArgs>? Cancel;

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Cornerstone.Presentation.Input.InputElement.KeyDown" /> event that occurs
        /// when a key is pressed while the drop-down portion of the
        /// <see cref="T:Cornerstone.Presentation.Controls.AutoCompleteBox" /> has focus.
        /// </summary>
        /// <param name="e">A <see cref="T:Cornerstone.Presentation.Input.KeyEventArgs" />
        /// that contains data about the
        /// <see cref="E:Cornerstone.Presentation.Input.InputElement.KeyDown" /> event.</param>
        void HandleKeyDown(KeyEventArgs e);
    }

}
