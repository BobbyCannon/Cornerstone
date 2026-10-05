using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Metadata for direct cornerstone properties.
    /// </summary>
    public class DirectPropertyMetadata<TValue> : PresentationPropertyMetadata, IDirectPropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="unsetValue">
        /// The value to use when the property is set to <see cref="PresentationProperty.UnsetValue"/>
        /// </param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        public DirectPropertyMetadata(
            TValue unsetValue = default!,
            BindingMode defaultBindingMode = BindingMode.Default,
            bool? enableDataValidation = null)
                : base(defaultBindingMode, enableDataValidation)
        {
            UnsetValue = unsetValue;
        }

        /// <summary>
        /// Gets the value to use when the property is set to <see cref="PresentationProperty.UnsetValue"/>.
        /// </summary>
        public TValue UnsetValue { get; private set; }


        /// <inheritdoc/>
        object? IDirectPropertyMetadata.UnsetValue => UnsetValue;

        /// <inheritdoc/>
        public override void Merge(PresentationPropertyMetadata baseMetadata, PresentationProperty property)
        {
            base.Merge(baseMetadata, property);

            if (baseMetadata is DirectPropertyMetadata<TValue> src)
            {
                UnsetValue ??= src.UnsetValue;
            }
        }

        /// <inheritdoc />
        public override PresentationPropertyMetadata GenerateTypeSafeMetadata()
        {
            if (IsReadOnly)
            {
                return this;
            }

            var copy = new DirectPropertyMetadata<TValue>(UnsetValue, DefaultBindingMode, EnableDataValidation);
            copy.Freeze();
            return copy;
        }
    }
}
