#region References

using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.PropertyGrid;

public class PropertyCellContext
{
	#region Fields

	private object _watchedTarget;
	private object _watchedTargetValue;

	#endregion

	#region Constructors

	public PropertyCellContext(object target, PropertyDescriptor property)
	{
		Target = target;
		Property = property;
	}

	#endregion

	#region Properties

	public Control EditorControl { get; set; }

	public PropertyCellFactory Factory { get; set; }

	public PropertyDescriptor Property { get; set; }

	public object Target { get; set; }

	#endregion

	#region Methods

	public void AddPropertyChangedObserver()
	{
		_watchedTarget = AddPropertyChangedObserver(Target);
		_watchedTargetValue = AddPropertyChangedObserver(Property.GetValue(Target));
	}

	public void RemovePropertyChangedObserver()
	{
		RemovePropertyChangedObserver(_watchedTarget);
		RemovePropertyChangedObserver(_watchedTargetValue);
	}

	private object AddPropertyChangedObserver(object value)
	{
		switch (value)
		{
			case INotifyPropertyChanged npc:
			{
				npc.PropertyChanged += OnPropertyChanged;
				return value;
			}
			case IEnumerable<INotifyPropertyChanged> npcc:
			{
				foreach (var n in npcc)
				{
					n.PropertyChanged += OnPropertyChanged;
				}
				return value;
			}
			default:
			{
				return null;
			}
		}
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == Property?.Name)
		{
			Factory?.HandlePropertyChanged(this);
		}
	}

	private void RemovePropertyChangedObserver(object value)
	{
		switch (value)
		{
			case INotifyPropertyChanged npc:
			{
				npc.PropertyChanged -= OnPropertyChanged;
				break;
			}
			case IEnumerable<INotifyPropertyChanged> npcc:
			{
				foreach (var n in npcc)
				{
					n.PropertyChanged -= OnPropertyChanged;
				}
				break;
			}
		}
	}

	#endregion
}