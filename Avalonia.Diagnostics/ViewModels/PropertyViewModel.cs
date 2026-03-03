#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public abstract class PropertyViewModel : ViewModel
{
	#region Fields

	private bool _isPinned;

	#endregion

	#region Properties

	public abstract Type AssignedType { get; }
	
	public abstract Type DeclaringType { get; }

	public string FullName => $"{GetType().Name.Replace("PropertyViewModel", "")}:{DeclaringType?.FullName}.{Name}";
	
	public abstract string Group { get; }
	
	public abstract bool? IsAttached { get; }

	public bool IsPinned
	{
		get => _isPinned;
		set => SetProperty(ref _isPinned, value);
	}

	public abstract bool IsReadonly { get; }

	public abstract object Key { get; }
	
	public abstract string Name { get; }
	
	public abstract string Priority { get; }
	
	public abstract Type PropertyType { get; }
	
	public string Type => PropertyType == AssignedType ? PropertyType.GetTypeName() : $"{PropertyType.GetTypeName()} {{{AssignedType.GetTypeName()}}}";
	
	public abstract object Value { get; set; }

	#endregion

	#region Methods

	public abstract void Update();

	#endregion
}