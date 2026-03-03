#region References

using System;
using Avalonia.Data;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class AvaloniaPropertyViewModel : PropertyViewModel
{
	#region Fields

	private Type _assignedType;
	private string _group;
	private string _priority;
	private readonly Type _propertyType;
	private readonly AvaloniaObject _target;
	private object _value;

	#endregion

	#region Constructors

	// Remove "nullable disable" after MemberNotNull will work on our CI.
	public AvaloniaPropertyViewModel(AvaloniaObject o, AvaloniaProperty property)
	{
		_target = o;
		Property = property;

		Name = property.IsAttached ? $"[{property.OwnerType.Name}.{property.Name}]" : property.Name;
		DeclaringType = property.OwnerType;
		_propertyType = property.PropertyType;
		Update();
	}

	#endregion

	#region Properties

	public override Type AssignedType => _assignedType;
	public override Type DeclaringType { get; }
	public override string Group => IsPinned ? "Pinned" : _group;
	public override bool? IsAttached => Property.IsAttached;
	public override bool IsReadonly => Property.IsReadOnly;
	public override object Key => Property;
	public override string Name { get; }
	public override string Priority => _priority;
	public AvaloniaProperty Property { get; }
	public override Type PropertyType => _propertyType;
	public override object Value
	{
		get => _value;
		set
		{
			try
			{
				_target.SetValue(Property, value);
				Update();
			}
			catch
			{
			}
		}
	}

	#endregion

	#region Methods

	// [MemberNotNull(nameof(_type), nameof(_group), nameof(_priority))]
	public override void Update()
	{
		if (Property.IsDirect)
		{
			object value;
			Type valueType = null;

			try
			{
				value = _target.GetValue(Property);
				valueType = value?.GetType();
			}
			catch (Exception e)
			{
				value = e.GetBaseException();
			}

			SetProperty(ref _value, value, nameof(Value));
			SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));
			SetProperty(ref _priority, "Direct", nameof(Priority));

			_group = "Properties";
		}
		else
		{
			object value;
			Type valueType = null;
			BindingPriority? priority = null;

			try
			{
				var diag = _target.GetDiagnostic(Property);

				value = diag.Value;
				valueType = value?.GetType();
				priority = diag.Priority;
			}
			catch (Exception e)
			{
				value = e.GetBaseException();
			}

			SetProperty(ref _value, value, nameof(Value));
			SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));

			if (priority != null)
			{
				SetProperty(ref _priority, priority.ToString()!, nameof(Priority));
				SetProperty(ref _group, IsAttached == true ? "Attached Properties" : "Properties", nameof(Group));
			}
			else
			{
				SetProperty(ref _priority, "Unset", nameof(Priority));
				SetProperty(ref _group, "Unset", nameof(Group));
			}
		}
		OnPropertyChanged(nameof(Type));
	}

	protected override void OnPropertyChanged(string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);
		if (propertyName == nameof(IsPinned))
		{
			OnPropertyChanged(nameof(Group));
		}
	}

	#endregion
}