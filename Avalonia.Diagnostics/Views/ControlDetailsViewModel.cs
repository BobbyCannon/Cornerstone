#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Diagnostics.ViewModels;
using Cornerstone.Avalonia.TreeDataGrid;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Dispatcher = Avalonia.Threading.Dispatcher;

#endregion

namespace Avalonia.Diagnostics.Views;

public class ControlDetailsViewModel : ViewModel, IDisposable, IClassesChangedListener
{
	#region Fields

	private readonly AvaloniaObject _avaloniaObject;
	private string _framesStatus;
	private readonly ISet<string> _pinnedProperties;
	private IDictionary<object, PropertyViewModel> _propertyIndex;
	private readonly Stack<(string Name, object Entry)> _selectedEntitiesStack;
	private object _selectedEntity;
	private string _selectedEntityName;
	private string _selectedEntityType;
	private PropertyViewModel _selectedProperty;
	private bool _showImplementedInterfaces;
	private bool _showInactiveFrames;
	private bool _snapshotFrames;

	#endregion

	#region Constructors

	public ControlDetailsViewModel(TreePageViewModel treePage, AvaloniaObject avaloniaObject, ISet<string> pinnedProperties)
	{
		_avaloniaObject = avaloniaObject;
		_pinnedProperties = pinnedProperties;
		_selectedEntitiesStack = new();

		Properties = new PresentationList<PropertyViewModel>(null,
			new OrderBy<PropertyViewModel>(x => x.IsPinned, true),
			new OrderBy<PropertyViewModel>(x => char.IsAsciiLetter(x.Name.First()), true),
			new OrderBy<PropertyViewModel>(x => x.Name)
		);

		PropertiesGridSource = new FlatTreeDataGridSource<PropertyViewModel>(Properties)
		{
			Columns =
			{
				new TemplateColumn<PropertyViewModel>("Name", "NameCell", null, new GridLength(1, GridUnitType.Auto),
					new TemplateColumnOptions<PropertyViewModel> { MinWidth = new GridLength(200) }),
				new TemplateColumn<PropertyViewModel>("PropertyValue", "PropertyValueCell", null, new GridLength(1, GridUnitType.Auto),
					new TemplateColumnOptions<PropertyViewModel> { MaxWidth = new GridLength(300) })
			}
		};
		TreePage = treePage;
		Layout = avaloniaObject is Visual visual
			? new ControlLayoutViewModel(visual)
			: null;

		//NavigateToProperty(_avaloniaObject, (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());

		AppliedFrames = [];
		PseudoClasses = [];

		if (avaloniaObject is StyledElement styledElement)
		{
			styledElement.Classes.AddListener(this);

			var pseudoClassAttributes = styledElement.GetType().GetCustomAttributes<PseudoClassesAttribute>(true);

			foreach (var classAttribute in pseudoClassAttributes)
			{
				foreach (var className in classAttribute.PseudoClasses)
				{
					PseudoClasses.Add(new PseudoClassViewModel(className, styledElement));
				}
			}

			var styleDiagnostics = styledElement.GetValueStoreDiagnostic();

			var clipboard = TopLevel.GetTopLevel(_avaloniaObject as Visual)?.Clipboard;

			foreach (var appliedStyle in styleDiagnostics.AppliedFrames.OrderBy(s => s.Priority))
			{
				AppliedFrames.Add(new ValueFrameViewModel(styledElement, appliedStyle, clipboard));
			}

			UpdateStyles();
		}
	}

	#endregion

	#region Properties

	public ObservableCollection<ValueFrameViewModel> AppliedFrames { get; }

	public bool CanNavigateToParentProperty => _selectedEntitiesStack.Count >= 1;

	public string FramesStatus
	{
		get => _framesStatus;
		set => SetProperty(ref _framesStatus, value);
	}

	public ControlLayoutViewModel Layout { get; }

	public PresentationList<PropertyViewModel> Properties { get; }

	public ITreeDataGridSource<PropertyViewModel> PropertiesGridSource { get; }

	public ObservableCollection<PseudoClassViewModel> PseudoClasses { get; }

	public object SelectedEntity
	{
		get => _selectedEntity;
		set => SetProperty(ref _selectedEntity, value);
	}

	public string SelectedEntityName
	{
		get => _selectedEntityName;
		set => SetProperty(ref _selectedEntityName, value);
	}

	public string SelectedEntityType
	{
		get => _selectedEntityType;
		set => SetProperty(ref _selectedEntityType, value);
	}

	public PropertyViewModel SelectedProperty
	{
		get => _selectedProperty;
		set => SetProperty(ref _selectedProperty, value);
	}

	public bool ShowInactiveFrames
	{
		get => _showInactiveFrames;
		set => SetProperty(ref _showInactiveFrames, value);
	}

	public bool SnapshotFrames
	{
		get => _snapshotFrames;
		set => SetProperty(ref _snapshotFrames, value);
	}

	public TreePageViewModel TreePage { get; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_avaloniaObject is INotifyPropertyChanged inpc)
		{
			inpc.PropertyChanged -= ControlPropertyChanged;
		}

		if (_avaloniaObject is AvaloniaObject ao)
		{
			ao.PropertyChanged -= ControlPropertyChanged;
		}

		if (_avaloniaObject is StyledElement se)
		{
			se.Classes.RemoveListener(this);
		}
	}

	public void NavigateToParentProperty()
	{
		if (_selectedEntitiesStack.Count > 0)
		{
			var property = _selectedEntitiesStack.Pop();
			NavigateToProperty(property.Entry, property.Name);
			OnPropertyChanged(nameof(CanNavigateToParentProperty));
		}
	}

	public void NavigateToSelectedProperty()
	{
		var selectedProperty = SelectedProperty;
		var selectedEntity = SelectedEntity;
		var selectedEntityName = SelectedEntityName;
		if (selectedEntity == null
			|| selectedProperty == null
			|| selectedProperty.PropertyType == typeof(string)
			|| selectedProperty.PropertyType.IsValueType)
		{
			return;
		}

		object property = null;

		switch (selectedProperty)
		{
			case AvaloniaPropertyViewModel avaloniaProperty:

				property = (_selectedEntity as Control)?.GetValue(avaloniaProperty.Property);

				break;

			case ClrPropertyViewModel clrProperty:
			{
				property = GetAllPublicProperties(selectedEntity.GetType())
					.FirstOrDefault(pi => clrProperty.Property == pi)?
					.GetValue(selectedEntity);

				break;
			}
		}

		if (property == null)
		{
			return;
		}

		_selectedEntitiesStack.Push((Name: selectedEntityName!, Entry: selectedEntity));

		var propertyName = selectedProperty.Name;

		//Strip out interface names
		if (propertyName.LastIndexOf('.') is var p && p != -1)
		{
			propertyName = propertyName.Substring(p + 1);
		}

		NavigateToProperty(property, selectedEntityName + "." + propertyName);

		OnPropertyChanged(nameof(CanNavigateToParentProperty));
	}

	public void SelectProperty(AvaloniaProperty property)
	{
		SelectedProperty = null;

		if (SelectedEntity != _avaloniaObject)
		{
			NavigateToProperty(
				_avaloniaObject,
				(_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());
		}

		if (PropertiesGridSource is null)
		{
			return;
		}

		foreach (var o in Properties)
		{
			if (o is AvaloniaPropertyViewModel propertyVm && propertyVm.Property == property)
			{
				SelectedProperty = propertyVm;
				break;
			}
		}
	}

	public void TogglePinnedProperty(object parameter)
	{
		if (parameter is PropertyViewModel model)
		{
			var fullname = model.FullName;
			if (_pinnedProperties.Contains(fullname))
			{
				_pinnedProperties.Remove(fullname);
				model.IsPinned = false;
			}
			else
			{
				_pinnedProperties.Add(fullname);
				model.IsPinned = true;
			}

			//PropertiesView?.Refresh();
			Properties.RefreshOrder();
		}
	}

	public void UpdatePropertiesView(bool showImplementedInterfaces)
	{
		_showImplementedInterfaces = showImplementedInterfaces;
		SelectedProperty = null;
		NavigateToProperty(_avaloniaObject, (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());
	}

	public void UpdateStyleFilters()
	{
		foreach (var style in AppliedFrames)
		{
			var hasVisibleSetter = false;

			foreach (var setter in style.Setters)
			{
				setter.IsVisible = TreePage.SettersFilter.Filter(setter.Name);

				hasVisibleSetter |= setter.IsVisible;
			}

			style.IsVisible = hasVisibleSetter;
		}
	}

	protected void NavigateToProperty(object o, string entityName)
	{
		var oldSelectedEntity = SelectedEntity;

		switch (oldSelectedEntity)
		{
			case AvaloniaObject ao1:
				ao1.PropertyChanged -= ControlPropertyChanged;
				break;

			case INotifyPropertyChanged inpc1:
				inpc1.PropertyChanged -= ControlPropertyChanged;
				break;
		}

		SelectedEntity = o;
		SelectedEntityName = entityName;
		SelectedEntityType = o.ToString();

		var properties = GetAvaloniaProperties(o)
			.Concat(GetClrProperties(o, _showImplementedInterfaces))
			.GroupBy(x => x.Name)
			.Select(x => x.FirstOrDefault())
			.Do(p => { p.IsPinned = _pinnedProperties.Contains(p.FullName); })
			.OrderBy(x => x.Name)
			.ToArray();

		_propertyIndex = properties
			.ToDictionary(x => x.Key, x => x);

		//var view = new DataGridCollectionView(properties);
		//view.GroupDescriptions.AddRange(GroupDescriptors);
		//view.SortDescriptions.AddRange(SortDescriptions);
		//view.Filter = FilterProperty;
		//PropertiesView = view;
		Properties.Reconcile(properties);

		switch (o)
		{
			case AvaloniaObject ao2:
				ao2.PropertyChanged += ControlPropertyChanged;
				break;

			case INotifyPropertyChanged inpc2:
				inpc2.PropertyChanged += ControlPropertyChanged;
				break;
		}
	}

	protected override void OnPropertyChanged(string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(SnapshotFrames))
		{
			if (!SnapshotFrames)
			{
				UpdateStyles();
			}
		}
	}

	void IClassesChangedListener.Changed()
	{
		if (!SnapshotFrames)
		{
			Dispatcher.UIThread.Post(UpdateStyles);
		}
	}

	private void ControlPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (_propertyIndex is not null && _propertyIndex.TryGetValue(e.Property, out var property))
		{
			property.Update();
		}

		Layout?.ControlPropertyChanged(sender, e);
	}

	private void ControlPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != null
			&& _propertyIndex is not null && _propertyIndex.TryGetValue(e.PropertyName, out var property))
		{
			property.Update();
		}

		if (!SnapshotFrames)
		{
			Dispatcher.UIThread.Post(UpdateStyles);
		}
	}

	private bool FilterProperty(object arg)
	{
		return !(arg is PropertyViewModel property) || TreePage.PropertiesFilter.Filter(property.Name);
	}

	private static IEnumerable<PropertyInfo> GetAllPublicProperties(Type type)
	{
		return type
			.GetProperties()
			.Concat(type.GetInterfaces().SelectMany(i => i.GetProperties()));
	}

	private static IEnumerable<PropertyViewModel> GetAvaloniaProperties(object o)
	{
		if (o is AvaloniaObject ao)
		{
			return AvaloniaPropertyRegistry.Instance.GetRegistered(ao)
				.Union(AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(ao.GetType()))
				.Select(x => new AvaloniaPropertyViewModel(ao, x));
		}
		return [];
	}

	private static IEnumerable<PropertyViewModel> GetClrProperties(object o, bool showImplementedInterfaces)
	{
		foreach (var p in GetClrProperties(o, o.GetType()))
		{
			yield return p;
		}

		if (showImplementedInterfaces)
		{
			foreach (var i in o.GetType().GetInterfaces())
			{
				foreach (var p in GetClrProperties(o, i))
				{
					yield return p;
				}
			}
		}
	}

	private static IEnumerable<PropertyViewModel> GetClrProperties(object o, Type t)
	{
		return t.GetProperties()
			.Where(x => x.GetIndexParameters().Length == 0)
			.Select(x => new ClrPropertyViewModel(o, x));
	}

	private void UpdateStyles()
	{
		var activeCount = 0;

		foreach (var style in AppliedFrames)
		{
			style.Update();

			if (style.IsActive)
			{
				activeCount++;
			}
		}

		var propertyBuckets = new Dictionary<AvaloniaProperty, List<SetterViewModel>>();

		foreach (var style in AppliedFrames.Reverse())
		{
			if (!style.IsActive)
			{
				continue;
			}

			foreach (var setter in style.Setters)
			{
				if (propertyBuckets.TryGetValue(setter.Property, out var setters))
				{
					foreach (var otherSetter in setters)
					{
						otherSetter.IsActive = false;
					}

					setter.IsActive = true;

					setters.Add(setter);
				}
				else
				{
					setter.IsActive = true;

					setters = [setter];

					propertyBuckets.Add(setter.Property, setters);
				}
			}
		}

		foreach (var pseudoClass in PseudoClasses)
		{
			pseudoClass.Update();
		}

		FramesStatus = $"Value Frames ({activeCount}/{AppliedFrames.Count} active)";
	}

	#endregion

	#region Classes

	private class PropertyComparer : IComparer<PropertyViewModel>, IComparer
	{
		#region Properties

		public static PropertyComparer Instance { get; } = new();

		#endregion

		#region Methods

		public int Compare(PropertyViewModel x, PropertyViewModel y)
		{
			if (x is null && y is null)
			{
				return 0;
			}

			if (x is null && y is not null)
			{
				return -1;
			}

			if (x is not null && y is null)
			{
				return 1;
			}

			var groupX = GroupIndex(x!.Group);
			var groupY = GroupIndex(y!.Group);

			if (groupX != groupY)
			{
				return groupX - groupY;
			}
			return string.CompareOrdinal(x.Name, y.Name);
		}

		public int Compare(object x, object y)
		{
			return Compare(x as PropertyViewModel, y as PropertyViewModel);
		}

		private static int GroupIndex(string group)
		{
			switch (group)
			{
				case "Pinned":
					return -1;
				case "Properties":
					return 0;
				case "Attached Properties":
					return 1;
				case "CLR Properties":
					return 2;
				default:
					return 3;
			}
		}

		#endregion
	}

	#endregion
}