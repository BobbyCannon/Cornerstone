namespace Cornerstone.Presentation;

/// <summary>
/// Represents an option for selection.
/// </summary>
/// <typeparam name="T"> The type of the ID. </typeparam>
public class SelectionOption<T> : Bindable
{
	#region Constructors

	/// <summary>
	/// Initializes an option for a selection.
	/// </summary>
	public SelectionOption() : this(default, "All")
	{
	}

	/// <summary>
	/// Initializes an option for a selection.
	/// </summary>
	/// <param name="id"> The ID value for the option. </param>
	/// <param name="name"> The name of the option. </param>
	public SelectionOption(T id, string name)
	{
		Id = id;
		Name = name;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The ID value for the option.
	/// </summary>
	public T Id { get; set; }

	/// <summary>
	/// The name of the option.
	/// </summary>
	public string Name { get; set; }

	#endregion
}