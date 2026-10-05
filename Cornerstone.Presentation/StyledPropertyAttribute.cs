#region References

using System;
using Cornerstone.Presentation.Data;

#endregion

namespace Cornerstone.Presentation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class StyledPropertyAttribute : Attribute
{
	#region Constructors

	public StyledPropertyAttribute() : this(null)
	{
	}

	public StyledPropertyAttribute(object defaultValue)
	{
		DefaultBindingMode = BindingMode.OneWay;
		DefaultValue = defaultValue;
	}

	#endregion

	#region Properties

	public string Coerce { get; set; }

	public BindingMode DefaultBindingMode { get; set; }

	public object DefaultValue { get; set; }

	public string DefaultValueCallback { get; set; }

	public bool EnableDataValidation { get; set; }

	public bool Inherits { get; set; }

	public string Validate { get; set; }

	#endregion
}
