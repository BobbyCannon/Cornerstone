#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Convert.Converters;

public class UpdateableConverter : BaseConverter
{
	#region Constructors

	public UpdateableConverter() : base(new Guid("0DC8145A-FE45-4FA5-9B94-CE9F6D43AA12"))
	{
	}

	#endregion

	#region Methods

	public override bool CanConvert(Type fromType, Type toType)
	{
		return toType.ImplementsType<IUpdateable>()
			&& toType.ImplementsType(fromType);
	}

	public override bool TryConvertTo(object from, Type fromType, Type toType, out object value, IConverterSettings settings = null)
	{
		if (SourceReflector.CreateInstance(toType) is IUpdateable response)
		{
			response.UpdateWith(from);
			value = response;
			return true;
		}

		return base.TryConvertTo(from, fromType, toType, out value, settings);
	}

	#endregion
}