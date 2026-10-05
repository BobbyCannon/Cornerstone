using System;
using System.ComponentModel;
using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Data
{
    /// <summary>
    /// A XAML binding to a property on a control's templated parent.
    /// </summary>
    public sealed partial class TemplateBinding : BindingBase
    {
        public TemplateBinding()
        {
        }

        public TemplateBinding([InheritDataTypeFrom(InheritDataTypeFromScopeKind.ControlTemplate)] PresentationProperty property)
        {
            Property = property;
        }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets the culture in which to evaluate the converter.
        /// </summary>
        /// <value>The default value is null.</value>
        /// <remarks>
        /// If this property is not set then <see cref="CultureInfo.CurrentCulture"/> will be used.
        /// </remarks>
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo? ConverterCulture { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the name of the source property on the templated parent.
        /// </summary>
        [ConstructorArgument("property")]
        [InheritDataTypeFrom(InheritDataTypeFromScopeKind.ControlTemplate)]
        public PresentationProperty? Property { get; set; }

        public BindingBase ProvideValue() => this;

        internal override BindingExpressionBase CreateInstance(PresentationObject target, PresentationProperty? targetProperty, object? anchor)
        {
            if (Mode is BindingMode.OneTime or BindingMode.OneWayToSource)
                throw new NotSupportedException("TemplateBinding does not support OneTime or OneWayToSource bindings.");

            return new TemplateBindingExpression(
                Property,
                Converter,
                ConverterCulture,
                ConverterParameter,
                Mode);
        }
    }
}
