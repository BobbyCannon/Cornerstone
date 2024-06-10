#region References

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Cornerstone.Data;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Validation;

/// <summary>
/// Validation for a typed object.
/// </summary>
public class Validator<T> : Validator
{
	#region Constructors

	/// <summary>
	/// Creates an instance of a Validator.
	/// </summary>
	public Validator() : this(null)
	{
	}

	/// <summary>
	/// Creates an instance of a Validator.
	/// </summary>
	public Validator(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Validate an object with provided test.
	/// </summary>
	/// <param name="validate"> The test to validate the object. </param>
	/// <param name="message"> The message for failed validation. </param>
	public Validator IsFalse(Func<T, bool> validate, string message)
	{
		return base.IsFalse(validate, message);
	}

	/// <summary>
	/// Validate an object with provided test.
	/// </summary>
	/// <param name="validate"> The test to validate the object. </param>
	/// <param name="message"> The message for failed validation. </param>
	public Validator IsTrue(Func<T, bool> validate, string message)
	{
		return base.IsTrue(validate, message);
	}

	/// <summary>
	/// Configure a validation for a property.
	/// </summary>
	public PropertyValidator<TProperty> Property<TProperty>(Expression<Func<T, TProperty>> expression)
	{
		return base.Property(expression);
	}

	#endregion
}

/// <summary>
/// Validation for an object.
/// </summary>
public class Validator : Bindable
{
	#region Constructors

	/// <summary>
	/// Creates an instance of a Validator.
	/// </summary>
	protected Validator(IDispatcher dispatcher) : base(dispatcher)
	{
		MemberValidators = new List<PropertyValidator>();
		Validations = new List<IValidation>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The validations for the object members.
	/// </summary>
	public IList<PropertyValidator> MemberValidators { get; }

	/// <summary>
	/// The validations for the object (as a whole).
	/// </summary>
	public IList<IValidation> Validations { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Validate an object with provided test.
	/// </summary>
	/// <param name="validate"> The test to validate the object. </param>
	/// <param name="message"> The message for failed validation. </param>
	public Validator IsFalse<T>(Func<T, bool> validate, string message)
	{
		Validations.Add(new Validation<T>(typeof(T).Name, message, x => !validate(x)));
		return this;
	}

	/// <summary>
	/// Validate an object with provided test.
	/// </summary>
	/// <param name="validate"> The test to validate the object. </param>
	/// <param name="message"> The message for failed validation. </param>
	public Validator IsTrue<T>(Func<T, bool> validate, string message)
	{
		Validations.Add(new Validation<T>(typeof(T).Name, message, validate));
		return this;
	}

	/// <summary>
	/// Configure a validation for a property.
	/// </summary>
	public PropertyValidator<TProperty> Property<T, TProperty>(Expression<Func<T, TProperty>> expression)
	{
		var propertyExpression = (MemberExpression) expression.Body;
		var response = new PropertyValidator<TProperty>((PropertyInfo) propertyExpression.Member);
		MemberValidators.Add(response);
		return response;
	}

	/// <summary>
	/// Throws a new exception.
	/// </summary>
	/// <typeparam name="T"> The type of exception to throw. </typeparam>
	/// <param name="message"> The message of the exception. </param>
	public static void ThrowException<T>(string message) where T : Exception, new()
	{
		var constructor = typeof(T).GetConstructor([typeof(string)]);
		var newException = (T) constructor?.Invoke([message])
			?? new Exception("Failed to throw custom validator exception due to missing constructor...");
		throw newException;
	}

	/// <summary>
	/// Runs the validator to check the parameter.
	/// </summary>
	public bool TryValidate(object update, out IList<FailedValidation> failures)
	{
		failures = new List<FailedValidation>();
		ProcessValidator(this, update, failures);
		return failures.Count <= 0;
	}

	/// <summary>
	/// Runs the validator to check the parameter.
	/// </summary>
	public bool TryValidate(PartialUpdate update, out IList<FailedValidation> failures)
	{
		failures = new List<FailedValidation>();
		ProcessValidator(this, update, failures);
		return failures.Count <= 0;
	}

	/// <summary>
	/// Runs the validator to check the parameter.
	/// </summary>
	public void Validate(object value)
	{
		if (value is PartialUpdate update)
		{
			Validate(update);
			return;
		}

		if (TryValidate(value, out var failures))
		{
			return;
		}

		var builder = new StringBuilder();

		foreach (var validation in failures)
		{
			builder.AppendLine(validation.Message);
		}

		ThrowException<ValidationException>(builder.ToString());
	}

	/// <summary>
	/// Runs the validator to check the partial update.
	/// </summary>
	public void Validate(PartialUpdate update)
	{
		if (TryValidate(update, out var failures))
		{
			return;
		}

		var builder = new StringBuilder();

		foreach (var validation in failures)
		{
			builder.AppendLine(validation.Message);
		}

		ThrowException<ValidationException>(builder.ToString());
	}

	/// <summary>
	/// Adds a validation for a property.
	/// </summary>
	internal PropertyValidator<TProperty> Add<TProperty>(PropertyValidator<TProperty> propertyValidator)
	{
		MemberValidators.Add(propertyValidator);
		return propertyValidator;
	}

	/// <summary>
	/// Process the validations.
	/// </summary>
	/// <param name="validations"> The list of validation to process. </param>
	/// <param name="value"> The value to process. </param>
	/// <param name="failedValidation"> The list of failed validations. </param>
	private static void ProcessValidations(IList<IValidation> validations, object value, ICollection<FailedValidation> failedValidation)
	{
		for (var i = 0; i < validations.Count; i++)
		{
			if (i >= validations.Count)
			{
				return;
			}

			var validation = validations[i];

			if (!validation.TryValidate(value))
			{
				failedValidation.Add(new FailedValidation(validation));
			}
		}
	}

	private static void ProcessValidator(Validator validator, PartialUpdate update, ICollection<FailedValidation> failedValidation)
	{
		ProcessValidations(validator.Validations, update, failedValidation);

		for (var i = 0; i < validator.MemberValidators.Count; i++)
		{
			if (i >= validator.MemberValidators.Count)
			{
				break;
			}

			var propertyValidator = validator.MemberValidators[i];
			var foundUpdate = update.TryGet(out var updateValue, propertyValidator.Info.Name);

			if (!foundUpdate)
			{
				if (!propertyValidator.MemberRequired)
				{
					continue;
				}

				failedValidation.Add(new FailedValidation(propertyValidator.Name, propertyValidator.MemberRequiredMessage));
				continue;
			}

			ProcessValidations(propertyValidator.Validations, updateValue, failedValidation);
		}
	}

	private static void ProcessValidator(Validator validator, object value, ICollection<FailedValidation> failedValidation)
	{
		if (value is PartialUpdate partialUpdate)
		{
			ProcessValidator(validator, partialUpdate, failedValidation);
			return;
		}

		ProcessValidations(validator.Validations, value, failedValidation);

		for (var i = 0; i < validator.MemberValidators.Count; i++)
		{
			if (i >= validator.MemberValidators.Count)
			{
				break;
			}

			var propertyValidator = validator.MemberValidators[i];
			var propertyValue = propertyValidator.Info.GetMemberValue(value);

			if ((propertyValue == null) && propertyValidator.MemberRequired)
			{
				failedValidation.Add(new FailedValidation(propertyValidator.Name, propertyValidator.MemberRequiredMessage));
			}

			ProcessValidations(propertyValidator.Validations, propertyValue, failedValidation);
		}
	}

	#endregion
}