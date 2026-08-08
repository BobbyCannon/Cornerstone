#region References

using System;
using System.Reflection;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class ClrPropertyViewModel : PropertyViewModel
{
	#region Fields

	private Type _assignedType;
	private readonly Type _propertyType;
	private readonly object _target;
	private object _value;

	#endregion

	#region Constructors

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

	#endregion

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

		var oldType = Type;
		SetProperty(ref _value, value, nameof(Value));
		SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));
		OnPropertyChanged(nameof(Type), oldType, Type);
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if (propertyName == nameof(IsPinned))
		{
			var groupValue = Group;
			OnPropertyChanged(nameof(Group), groupValue, groupValue);
		}
	}

	#endregion
}