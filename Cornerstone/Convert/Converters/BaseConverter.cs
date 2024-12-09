#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Convert.Converters;

/// <summary>
/// Represents a converter that can convert from one type to another.
/// </summary>
public abstract class BaseConverter : IProvider
{
    #region Fields

    private readonly Guid _id;

    #endregion

    #region Constructors

    /// <summary>
    /// Initialize the type converter.
    /// </summary>
    protected BaseConverter(Guid id) : this(id, Array.Empty<Type>(), Array.Empty<Type>())
    {
    }

    /// <summary>
    /// Initialize the type converter.
    /// </summary>
    /// <param name="id"> The ID of this converter. </param>
    /// <param name="primaryTypes"> The types this converter is primarily supporting. </param>
    /// <param name="conversionTypes"> The types this converter can convert to. </param>
    protected BaseConverter(Guid id, Type[] primaryTypes, Type[] conversionTypes)
    {
        _id = id;

        FromTypes = new ReadOnlySet<Type>(primaryTypes);
        ToTypes = new ReadOnlySet<Type>(conversionTypes);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Types this converter can convert to / from. Meaning the non primary type must be one of these types.
    /// </summary>
    public ReadOnlySet<Type> ToTypes { get; }

    /// <summary>
    /// Types this converter is primarily for. Meaning the To/From must be one of these types.
    /// </summary>
    public ReadOnlySet<Type> FromTypes { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Check to see if this type converter can convert from the provided type.
    /// </summary>
    /// <param name="fromType"> The type to convert from. </param>
    /// <param name="toType"> The type to convert to. </param>
    /// <returns> True if the converter can convert from the type otherwise false. </returns>
    public virtual bool CanConvert(Type fromType, Type toType)
    {
        return FromTypes.Contains(fromType)
            && ToTypes.Contains(toType);
    }

    /// <inheritdoc />
    public Guid GetProviderId()
    {
        return _id;
    }

	/// <summary>
	/// Try and convert an object to the provided type.
	/// </summary>
	/// <param name="from"> The object to convert from. </param>
	/// <param name="fromType"> The type to convert from. </param>
	/// <param name="toType"> The type to convert to. </param>
	/// <param name="value"> The new converted object. </param>
	/// <param name="settings"> The options for converting. </param>
	/// <returns> True if the item was converted otherwise false. </returns>
	public virtual bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
    {
        value = default;
        return false;
    }

    #endregion
}