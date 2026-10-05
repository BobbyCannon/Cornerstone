namespace Cornerstone.Presentation.Controls.Primitives
{
    /// <summary>    
    /// Specifies the type of Cornerstone.Presentation.Controls.ScrollBar.Scroll event
    /// that occurred.
    /// </summary>
    public enum ScrollEventType
    {
        /// <summary>    
        /// Specifies that the Cornerstone.Presentation.Controls.Thumb moved a specified
        /// distance, as determined by the value of Cornerstone.Presentation.Controls.RangeBase.SmallChange.
        /// The Cornerstone.Presentation.Controls.Thumb moved to the left for a horizontal
        /// Cornerstone.Presentation.Controls.ScrollBar or upward for a vertical Cornerstone.Presentation.Controls.ScrollBar.
        /// </summary>
        SmallDecrement = 0,
        /// <summary>    
        /// Specifies that the Cornerstone.Presentation.Controls.Thumb moved a specified
        /// distance, as determined by the value of Cornerstone.Presentation.Controls.RangeBase.SmallChange.
        /// The Cornerstone.Presentation.Controls.Thumb moved to the right for a horizontal
        /// Cornerstone.Presentation.Controls.ScrollBar or downward for a vertical Cornerstone.Presentation.Controls.ScrollBar.
        /// </summary>
        SmallIncrement = 1,
        /// <summary>    
        /// Specifies that the Cornerstone.Presentation.Controls.Thumb moved a specified
        /// distance, as determined by the value of Cornerstone.Presentation.Controls.RangeBase.LargeChange.
        /// The Cornerstone.Presentation.Controls.Thumb moved to the left for a horizontal
        /// Cornerstone.Presentation.Controls.ScrollBar or upward for a vertical Cornerstone.Presentation.Controls.ScrollBar.
        /// </summary>
        LargeDecrement = 2,
        /// <summary>    
        /// Specifies that the Cornerstone.Presentation.Controls.Thumb moved a specified
        /// distance, as determined by the value of Cornerstone.Presentation.Controls.RangeBase.LargeChange.
        /// The Cornerstone.Presentation.Controls.Thumb moved to the right for a horizontal
        /// Cornerstone.Presentation.Controls.ScrollBar or downward for a vertical Cornerstone.Presentation.Controls.ScrollBar.
        /// </summary>
        LargeIncrement = 3,
        /// <summary>    
        /// The Cornerstone.Presentation.Controls.Thumb was dragged and caused a Cornerstone.Presentation.UIElement.MouseMove
        /// event. A Cornerstone.Presentation.Controls.ScrollBar.Scroll event of this Cornerstone.Presentation.Controls.Primitives.ScrollEventType
        /// may occur more than one time when the Cornerstone.Presentation.Controls.Thumb
        /// is dragged in the Cornerstone.Presentation.Controls.ScrollBar.
        /// </summary>
        ThumbTrack = 4,
        /// <summary>    
        /// Specifies that the Cornerstone.Presentation.Controls.Thumb was dragged to a
        /// new position and is now no longer being dragged by the user.
        /// </summary>
        EndScroll = 5
    }
}
