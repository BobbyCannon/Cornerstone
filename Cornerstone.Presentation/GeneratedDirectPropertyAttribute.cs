using System;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation;

/// <summary>
/// Instructs the Cornerstone property source generator to emit a <see cref="DirectProperty{TOwner, TValue}"/>
/// registration and accessor bodies for the annotated partial property.
/// </summary>
/// <remarks>
/// <para>
/// Apply to a <c>partial</c> instance property with both <c>get</c> and <c>set</c> accessors,
/// declared on a <c>partial</c> class deriving from <see cref="PresentationObject"/>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class GeneratedDirectPropertyAttribute : Attribute
{
    /// <summary>
    /// Re-owns an existing direct property from the specified base type instead of registering a new property.
    /// The source property must be a static <c>{Name}Property</c> member of the given type.
    /// </summary>
    public Type? AddOwnerFrom { get; set; }

    /// <summary>
    /// The value returned when the property is cleared (e.g. by <c>ClearValue</c>)
    /// or when a binding produces an invalid value.
    /// The initial value of the property comes from the property initializer.
    /// </summary>
    public object? UnsetValue { get; set; }

    /// <summary>
    /// The default binding mode for the property.
    /// </summary>
    public BindingMode DefaultBindingMode { get; set; }

    /// <summary>
    /// Whether the property is interested in data validation.
    /// </summary>
    public bool EnableDataValidation { get; set; }
}
