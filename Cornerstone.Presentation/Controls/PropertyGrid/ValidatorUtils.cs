#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

#endregion

namespace Cornerstone.Presentation.Controls.PropertyGrid;

public static class ValidatorUtils
{
	#region Methods

	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "PropertyGrid validates runtime objects via TypeDescriptor and DataAnnotations.")]
	public static bool TryValidateObject(object target, out string message)
	{
		foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(target))
		{
			if (property.IsDefined<ValidationAttribute>())
			{
				var results = new List<ValidationResult>();
				var value = property.GetValue(target);

				if ((value != null) && !Validator.TryValidateValue(
						value,
						new ValidationContext(value)
						{
							DisplayName = property.DisplayName,
							MemberName = property.Name
						},
						results,
						property.GetCustomAttributes<ValidationAttribute>()
					))
				{
					message = string.Join(Environment.NewLine, results.Select(x => x.GetDisplayMessage()));
					return false;
				}
			}
		}

		message = string.Empty;
		return true;
	}

	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "PropertyGrid validates runtime objects via TypeDescriptor and DataAnnotations.")]
	public static bool TryValidateProperty(object component, PropertyDescriptor property, out string message)
	{
		if (property.IsDefined<ValidationAttribute>())
		{
			var results = new List<ValidationResult>();
			var value = property.GetValue(component);

			if (value != null)
			{
				if (!Validator.TryValidateValue(
						value,
						new ValidationContext(value)
						{
							DisplayName = property.DisplayName,
							MemberName = property.Name
						},
						results,
						property.GetCustomAttributes<ValidationAttribute>()
					))
				{
					message = string.Join(Environment.NewLine, results.Select(x => x.GetDisplayMessage()));
					return false;
				}
			}
			else
			{
				var builder = new StringBuilder();
				var hasError = false;

				foreach (var attr in property.GetCustomAttributes<ValidationAttribute>())
				{
					if (!attr.IsValid(value))
					{
						hasError = true;
						builder.AppendLine(attr.FormatErrorMessage(property.DisplayName));
					}
				}

				if (hasError)
				{
					message = builder.ToString().Trim();
					return false;
				}
			}
		}

		message = string.Empty;
		return true;
	}

	#endregion
}