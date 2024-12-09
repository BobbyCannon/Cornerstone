namespace Cornerstone.Data;

/// <summary>
/// Represents options provider for IUpdateable.
/// </summary>
public interface IUpdateableOptionsProvider
{
	#region Methods

	/// <summary>
	/// Get options for the updateable type.
	/// </summary>
	public IncludeExcludeSettings GetUpdateableOptions(UpdateableAction action);

	#endregion
}