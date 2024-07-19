#region References

using System;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class EnumConverters
{
	#region Fields

	public static readonly FuncValueConverter<object, object, bool> IsEqual = new(Convert);

	#endregion

	#region Methods

	public static bool Convert(object value, object parameter)
	{
		if (value is not Enum eValue)
		{
			return false;
		}

		if (parameter is Enum pValue)
		{
			return Equals(eValue, pValue);
		}

		if (parameter is int iValue)
		{
			var nValue = System.Convert.ChangeType(eValue, eValue.GetTypeCode());
			return Equals(nValue, iValue);
		}
		
		if (parameter is string sValue)
		{
			return Equals(eValue.ToString(), sValue);
		}

		return false;
	}

	#endregion
}