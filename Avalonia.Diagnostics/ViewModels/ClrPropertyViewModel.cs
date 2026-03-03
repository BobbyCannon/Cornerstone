#region References

using System;
using System.ComponentModel;
using System.Reflection;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class ClrPropertyViewModel : PropertyViewModel
{
	private readonly object _target;
	private Type _assignedType;
	private object _value;
	private readonly Type _propertyType;

	// Remove "nullable disable" after MemberNotNull will work on our CI.
	public ClrPropertyViewModel(object o, PropertyInfo property)
	{
		_target = o;
		Property = property;

		if ((property.DeclaringType == null) || !property.DeclaringType.IsInterface)
		{
			Name = property.Name;
		}
		else
		{
			Name = property.DeclaringType.Name + '.' + property.Name;
		}

		DeclaringType = property.DeclaringType;
		_propertyType = property.PropertyType;

		Update();
	}

	#region Properties

	public override Type AssignedType => _assignedType;

	public override Type DeclaringType { get; }
	public override string Group => IsPinned ? "Pinned" : "CLR Properties";

	public override bool? IsAttached => null;
	public override bool IsReadonly => !Property.CanWrite;
	public override object Key => Name;
	public override string Name { get; }

	public override string Priority => string.Empty;

	public PropertyInfo Property { get; }
	public override Type PropertyType => _propertyType;

	public override object Value
	{
		get => _value;
		set
		{
			try
			{
				Property.SetValue(_target, value);
				Update();
			}
			catch
			{
			}
		}
	}

	#endregion

	#region Methods

	// [MemberNotNull(nameof(_type))]
	public override void Update()
	{
		object value;
		Type valueType = null;

		try
		{
			value = Property.GetValue(_target);
			valueType = value?.GetType();
		}
		catch (Exception e)
		{
			value = e.GetBaseException();
		}

		SetProperty(ref _value, value, nameof(Value));
		SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));
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