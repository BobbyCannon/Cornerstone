namespace Cornerstone.Validation;

/// <summary>
/// Represents a failed validation.
/// </summary>
public class FailedValidation
{
	#region Constructors

	/// <summary>
	/// Initialize the failed validation message.
	/// </summary>
	/// <param name="validation"> The validation that failed. </param>
	public FailedValidation(IValidation validation) : this(validation.Name, validation.Message)
	{
	}

	/// <summary>
	/// Initialize the failed validation message.
	/// </summary>
	/// <param name="name"> The name of the validation that failed. </param>
	/// <param name="message"> The message of the failed validation. </param>
	public FailedValidation(string name, string message)
	{
		Name = name;
		Message = message;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The message for failed validation.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// The name for failed validation.
	/// </summary>
	public string Name { get; }

	#endregion
}