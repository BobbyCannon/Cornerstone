namespace Cornerstone.Presentation.Diagnostics
{
    /// <summary>
    /// Defines diagnostic extensions on <see cref="PresentationObject"/>s.
    /// </summary>
    public static class PresentationObjectExtensions
    {
        /// <summary>
        /// Gets a diagnostic for a <see cref="PresentationProperty"/> on a <see cref="PresentationObject"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// A <see cref="PresentationPropertyValue"/> that can be used to diagnose the state of the
        /// property on the object.
        /// </returns>
        public static PresentationPropertyValue GetDiagnostic(this PresentationObject o, PresentationProperty property)
        {
            return o.GetDiagnosticInternal(property);
        }

        /// <summary>
        /// Gets a value store diagnostics for a <see cref="PresentationObject"/>.
        /// </summary>
        /// <param name="cornerstoneObject">The cornerstone object.</param>
        public static ValueStoreDiagnostic GetValueStoreDiagnostic(this PresentationObject cornerstoneObject)
        {
            return cornerstoneObject.GetValueStore().GetStoreDiagnostic();
        }
    }
}
