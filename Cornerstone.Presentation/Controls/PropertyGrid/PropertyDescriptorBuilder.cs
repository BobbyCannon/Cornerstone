#region References

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid;

public class PropertyDescriptorBuilder
{
	#region Fields

	public readonly object Target;

	#endregion

	#region Constructors

	public PropertyDescriptorBuilder(object target)
	{
		Target = target ?? throw new ArgumentNullException(nameof(target));
	}

	#endregion

	#region Methods

	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "PropertyGrid enumerates runtime object properties via TypeDescriptor.")]
	public PropertyDescriptorCollection GetProperties()
	{
		if (Target is ICustomTypeDescriptor ctd)
		{
			return ctd.GetProperties();
		}

		return TypeDescriptor.GetProperties(Target);
	}

	#endregion
}