#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Options for an updateable object.
/// </summary>
public class UpdateableOptions : IncludeExcludeOptions
{
	#region Constructors

	/// <summary>
	/// Initializes options.
	/// </summary>
	/// <param name="action"> The updateable action these options are for. </param>
	/// <param name="including"> The parameters to include when updating. </param>
	/// <param name="excluding"> The parameters to exclude when updating. </param>
	public UpdateableOptions(UpdateableAction action, IEnumerable<string> including, IEnumerable<string> excluding)
		: this(action, including, excluding, true)
	{
	}

	/// <summary>
	/// Initializes options.
	/// </summary>
	/// <param name="action"> The updateable action these options are for. </param>
	/// <param name="including"> The parameters to include when updating. </param>
	/// <param name="excluding"> The parameters to exclude when updating. </param>
	/// <param name="ignoreCase"> If true then include / exclude collections are case-insensitive. </param>
	public UpdateableOptions(UpdateableAction action, IEnumerable<string> including, IEnumerable<string> excluding, bool ignoreCase)
		: base(including, excluding, ignoreCase)
	{
		Action = action;
	}

	
	#endregion

	#region Properties

	/// <summary>
	/// The updateable action these options are for.
	/// </summary>
	public UpdateableAction Action { get; }

	#endregion
}