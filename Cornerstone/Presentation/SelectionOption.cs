namespace Cornerstone.Presentation;

/// <summary>
/// Represents an option for selection.
/// </summary>
/// <typeparam name="T"> The type of the ID. </typeparam>
public class SelectionOption<T> : CornerstoneObject
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
	public SelectionOption(T id, string name) : this(id, name, false)
	{
	}

	/// <summary>
	/// Initializes an option for a selection.
	/// </summary>
	/// <param name="id"> The ID value for the option. </param>
	/// <param name="name"> The name of the option. </param>
	/// <param name="isChecked"> True if this option is the selected item. </param>
	public SelectionOption(T id, string name, bool isChecked)
	{
		Id = id;
		Name = name;
		IsChecked = isChecked;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The ID value for the option.
	/// </summary>
	public T Id { get; set; }

	/// <summary>
	/// True if this option is the selected item.
	/// </summary>
	public bool IsChecked
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}
			field = value;
			OnPropertyChanged(nameof(IsChecked), !value, value);
		}
	}

	/// <summary>
	/// The name of the option.
	/// </summary>
	public string Name { get; set; }

	#endregion
}