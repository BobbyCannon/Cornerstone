using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Collections;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// A collection of <see cref="KeyFrame"/>s.
    /// </summary>
    public sealed class KeyFrames : OldPresentationList<KeyFrame>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyFrames"/> class.
        /// </summary>
        public KeyFrames()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyFrames"/> class.
        /// </summary>
        /// <param name="items">The initial items in the collection.</param>
        public KeyFrames(IEnumerable<KeyFrame> items)
            : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}