using System;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Exception signifying an internal logic error in Cornerstone.Presentation.
    /// </summary>
    public class PresentationInternalException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationInternalException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public PresentationInternalException(string message)
            : base(message)
        {
        }
    }
}
