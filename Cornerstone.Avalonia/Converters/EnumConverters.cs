#region References

using System;
using Avalonia.Data.Converters;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class EnumConverters
{
	#region Fields

	public static readonly FuncValueConverter<Enum, Enum, bool> HasFlag;
	public static readonly FuncValueConverter<object, object, bool> IsEqual;
	public static readonly FuncValueConverter<object, string> ToShortName;

	#endregion

	#region Constructors

	static EnumConverters()
	{
		IsEqual = new(Convert);
		HasFlag = new(ComputeHasFlag);
		ToShortName = new(GetShortName);
	}

	#endregion

	#region Methods

	public static bool ComputeHasFlag(Enum value, Enum parameter)
	{
		return value.HasFlag(parameter);
	}

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

	public static string GetShortName(object value)
	{
		if (value is Enum eValue)
		{
			return eValue.GetDisplayShortName();
		}

		return value.ToString();
	}

	#endregion
}