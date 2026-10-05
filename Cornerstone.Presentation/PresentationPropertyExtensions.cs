using Cornerstone.Presentation.Media;

#nullable enable

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Extensions for <see cref="PresentationProperty"/>.
    /// </summary>
    internal static class PresentationPropertyExtensions
    {
        /// <summary>
        /// Checks if values of given property can affect rendering (via <see cref="IAffectsRender"/>).
        /// </summary>
        /// <param name="property">Property to check.</param>
        public static bool CanValueAffectRender(this PresentationProperty property)
        {
            var propertyType = property.PropertyType;

            // Only case that we are sure that property value CAN'T affect render are sealed types that don't implement
            // the interface.
            var cannotAffectRender = propertyType.IsSealed && !typeof(IAffectsRender).IsAssignableFrom(propertyType);

            return !cannotAffectRender;
        }
    }
}
