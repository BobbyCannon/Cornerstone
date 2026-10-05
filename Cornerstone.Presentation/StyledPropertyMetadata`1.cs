using System;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Metadata for styled cornerstone properties.
    /// </summary>
    public class StyledPropertyMetadata<TValue> : PresentationPropertyMetadata, IStyledPropertyMetadata
    {
        private Optional<TValue> _defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyMetadata{TValue}"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="coerce">A value coercion callback.</param>
        /// <param name="enableDataValidation">Whether the property is interested in data validation.</param>
        public StyledPropertyMetadata(
            Optional<TValue> defaultValue = default,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<PresentationObject, TValue, TValue>? coerce = null,
            bool enableDataValidation = false)
                : base(defaultBindingMode, enableDataValidation)
        {
            _defaultValue = defaultValue;
            CoerceValue = coerce;
        }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        public TValue DefaultValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _defaultValue.GetValueOrDefault()!;
        }

        /// <summary>
        /// Gets the value coercion callback, if any.
        /// </summary>
        public Func<PresentationObject, TValue, TValue>? CoerceValue { get; private set; }

        object? IStyledPropertyMetadata.DefaultValue => DefaultValue;

        /// <inheritdoc/>
        public override void Merge(PresentationPropertyMetadata baseMetadata, PresentationProperty property)
        {
            base.Merge(baseMetadata, property);

            if (baseMetadata is StyledPropertyMetadata<TValue> src)
            {
                if (!_defaultValue.HasValue)
                {
                    _defaultValue = src.DefaultValue;
                }

                if (CoerceValue == null)
                {
                    CoerceValue = src.CoerceValue;
                }
            }
        }

        /// <inheritdoc />
        public override PresentationPropertyMetadata GenerateTypeSafeMetadata()
        {
            if (IsReadOnly && CoerceValue is null)
            {
                return this;
            }

            var copy = new StyledPropertyMetadata<TValue>(DefaultValue, DefaultBindingMode, null, EnableDataValidation ?? false);
            copy.Freeze();
            return copy;
        }
    }
}
