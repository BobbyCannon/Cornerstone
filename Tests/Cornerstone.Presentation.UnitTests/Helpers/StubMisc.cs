#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Theming;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Internal;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.Styling;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubDisposable : IDisposable
{
	#region Constructors

	public StubDisposable()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	#endregion

	#region Methods

	public void Dispose()
	{
		Calls.Add(nameof(Dispose));
	}

	#endregion
}

internal sealed class StubTransition : ITransition
{
	#region Constructors

	public StubTransition()
	{
		Calls = new StubCallLog();
		ApplyResult = new StubDisposable();
	}

	#endregion

	#region Properties

	public Action<Animatable, IClock, object, object> ApplyHandler { get; set; }

	public StubDisposable ApplyResult { get; set; }

	public StubCallLog Calls { get; }

	public PresentationProperty Property { get; set; }

	#endregion

	#region Methods

	public IDisposable Apply(Animatable control, IClock clock, object oldValue, object newValue)
	{
		Calls.Add("Apply", control, clock, oldValue, newValue);
		ApplyHandler?.Invoke(control, clock, oldValue, newValue);
		return ApplyResult;
	}

	IDisposable ITransition.Apply(Animatable control, IClock clock, object oldValue, object newValue)
	{
		return Apply(control, clock, oldValue, newValue);
	}

	#endregion
}

public sealed class StubValueConverter : IValueConverter
{
	#region Constructors

	public StubValueConverter()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		Calls.Add(nameof(Convert), value, targetType, parameter, culture);
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		Calls.Add(nameof(ConvertBack), value, targetType, parameter, culture);
		return value;
	}

	#endregion
}

public sealed class StubPageTransition : IPageTransition
{
	#region Constructors

	public StubPageTransition()
	{
		Calls = new StubCallLog();
		StartResult = Task.CompletedTask;
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public Func<Visual, Visual, bool, CancellationToken, Task> StartHandler { get; set; }

	public Task StartResult { get; set; }

	#endregion

	#region Methods

	public Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
	{
		Calls.Add(nameof(Start), from, to, forward, cancellationToken);
		if (StartHandler != null)
		{
			return StartHandler(from, to, forward, cancellationToken);
		}

		return StartResult;
	}

	#endregion
}

public class StubResourceHost : IResourceHost
{
	#region Constructors

	public StubResourceHost()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public bool HasResources { get; set; }

	#endregion

	#region Methods

	public void NotifyHostedResourcesChanged(ResourcesChangedEventArgs e)
	{
		Calls.Add(nameof(NotifyHostedResourcesChanged), e);
	}

	public bool TryGetResource(object key, ThemeVariant theme, out object value)
	{
		value = null;
		return false;
	}

	#endregion

	#region Events

	public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged
	{
		add { }
		remove { }
	}

	#endregion
}

public sealed class StubThemeVariantHost : StubResourceHost, IThemeVariantHost
{
	#region Properties

	public ThemeVariant ActualThemeVariant { get; set; }

	#endregion

	#region Events

	public event EventHandler ActualThemeVariantChanged
	{
		add { }
		remove { }
	}

	#endregion
}

public sealed class StubHeadered : IHeadered
{
	#region Properties

	public object Header { get; set; }

	#endregion
}

public sealed class StubPlatformLifetimeEvents : IPlatformLifetimeEventsImpl
{
	#region Fields

	private EventHandler<ShutdownRequestedEventArgs> _shutdownRequested;

	#endregion

	#region Methods

	public void Raise(Action<StubPlatformLifetimeEvents> eventBinder, params object[] args)
	{
		if ((args != null) && (args.Length > 0) && args[0] is ShutdownRequestedEventArgs e)
		{
			_shutdownRequested?.Invoke(this, e);
		}
	}

	public void RaiseShutdownRequested(ShutdownRequestedEventArgs e)
	{
		_shutdownRequested?.Invoke(this, e);
	}

	#endregion

	#region Events

	public event EventHandler<ShutdownRequestedEventArgs> ShutdownRequested
	{
		add => _shutdownRequested += value;
		remove => _shutdownRequested -= value;
	}

	#endregion
}

public sealed class StubMainMenu : IMainMenu
{
	#region Constructors

	public StubMainMenu()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public bool IsOpen { get; set; }

	#endregion

	#region Methods

	public void Close()
	{
		Calls.Add(nameof(Close));
	}

	public void Open()
	{
		Calls.Add(nameof(Open));
		IsOpen = true;
	}

	#endregion

	#region Events

	public event EventHandler<RoutedEventArgs> Closed
	{
		add { }
		remove { }
	}

	#endregion
}

internal sealed class StubMenu : Control, IMenu, IMainMenu
{
	#region Fields

	private IMenuItem _selectedItem;
	private readonly IEnumerable<IMenuItem> _subItems;

	#endregion

	#region Constructors

	public StubMenu()
	{
		Calls = new StubCallLog();
		_subItems = Array.Empty<IMenuItem>();
		MoveSelectionResult = true;
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public IMenuInteractionHandler InteractionHandler { get; set; }

	public bool IsOpen { get; set; }

	public Action<NavigationDirection, bool> MoveSelectionHandler { get; set; }

	public bool MoveSelectionResult { get; set; }

	public IMenuItem SelectedItem
	{
		get => _selectedItem;
		set
		{
			Calls.Add("set_SelectedItem", value);
			_selectedItem = value;
		}
	}

	IMenuItem IMenuElement.SelectedItem
	{
		get => SelectedItem;
		set => SelectedItem = value;
	}

	IEnumerable<IMenuItem> IMenuElement.SubItems => _subItems;

	TopLevel IMenu.TopLevel => VisualRoot as TopLevel;

	#endregion

	#region Methods

	public void Close()
	{
		Calls.Add(nameof(Close));
		IsOpen = false;
	}

	public bool MoveSelection(NavigationDirection direction, bool wrap)
	{
		Calls.Add(nameof(MoveSelection), direction, wrap);
		MoveSelectionHandler?.Invoke(direction, wrap);
		return MoveSelectionResult;
	}

	public void Open()
	{
		Calls.Add(nameof(Open));
		IsOpen = true;
	}

	#endregion

	#region Events

	public event EventHandler<RoutedEventArgs> Closed
	{
		add { }
		remove { }
	}

	#endregion
}

internal sealed class StubMenuItem : Control, IInputElement, IMenuItem
{
	#region Fields

	private IMenuItem _selectedItem;
	private IEnumerable<IMenuItem> _subItems;

	#endregion

	#region Constructors

	public StubMenuItem()
	{
		Calls = new StubCallLog();
		_subItems = Array.Empty<IMenuItem>();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public string GroupName { get; set; }

	public bool HasSubMenu { get; set; }

	public bool IsChecked { get; set; }

	public bool IsPointerOverSubMenu { get; set; }

	public bool IsSubMenuOpen { get; set; }

	public bool IsTopLevel { get; set; }

	public IMenuElement MenuParent { get; set; }

	public IMenuItem SelectedItem
	{
		get => _selectedItem;
		set
		{
			Calls.Add("set_SelectedItem", value);
			_selectedItem = value;
		}
	}

	public bool StaysOpenOnClick { get; set; }

	public IEnumerable<IMenuItem> SubItems
	{
		get => _subItems;
		set => _subItems = value ?? Array.Empty<IMenuItem>();
	}

	public MenuItemToggleType ToggleType { get; set; }

	IMenuElement IMenuItem.Parent => MenuParent;

	IMenuItem IMenuElement.SelectedItem
	{
		get => SelectedItem;
		set => SelectedItem = value;
	}

	IEnumerable<IMenuItem> IMenuElement.SubItems => _subItems;

	#endregion

	#region Methods

	public void Close()
	{
		Calls.Add(nameof(Close));
	}

	public bool MoveSelection(NavigationDirection direction, bool wrap)
	{
		Calls.Add(nameof(MoveSelection), direction, wrap);
		return false;
	}

	public void Open()
	{
		Calls.Add(nameof(Open));
	}

	public void RaiseClick()
	{
		Calls.Add(nameof(RaiseClick));
	}

	public void SetParent(IMenuElement parent)
	{
		MenuParent = parent;
	}

	bool IInputElement.Focus(NavigationMethod method, KeyModifiers keyModifiers)
	{
		Calls.Add(nameof(IInputElement.Focus), method, keyModifiers);
		return true;
	}

	#endregion
}

public sealed class StubPopupPositioner : IPopupPositioner
{
	#region Constructors

	public StubPopupPositioner()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	#endregion

	#region Methods

	public void Update(PopupPositionerParameters parameters)
	{
		Calls.Add(nameof(Update), parameters);
	}

	#endregion
}

public sealed class StubCustomDrawOperation : ICustomDrawOperation
{
	#region Constructors

	public StubCustomDrawOperation()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public Rect Bounds { get; set; }

	public StubCallLog Calls { get; }

	public Func<Point, bool> HitTestHandler { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		Calls.Add(nameof(Dispose));
	}

	public bool Equals(ICustomDrawOperation other)
	{
		return ReferenceEquals(this, other);
	}

	public bool HitTest(Point p)
	{
		return HitTestHandler != null ? HitTestHandler(p) : Bounds.Contains(p);
	}

	public void Render(ImmediateDrawingContext context)
	{
		Calls.Add(nameof(Render), context);
	}

	#endregion
}

public sealed class StubStyle : IStyle, IResourceProvider
{
	#region Constructors

	public StubStyle()
	{
		Calls = new StubCallLog();
		Children = Array.Empty<IStyle>();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public IReadOnlyList<IStyle> Children { get; set; }

	public bool HasResources { get; set; }

	public IResourceHost Owner { get; private set; }

	#endregion

	#region Methods

	public void AddOwner(IResourceHost owner)
	{
		Calls.Add(nameof(AddOwner), owner);
		Owner = owner;
	}

	public void RemoveOwner(IResourceHost owner)
	{
		Calls.Add(nameof(RemoveOwner), owner);
		if (ReferenceEquals(Owner, owner))
		{
			Owner = null;
		}
	}

	public bool TryGetResource(object key, ThemeVariant theme, out object value)
	{
		value = null;
		return false;
	}

	#endregion

	#region Events

	public event EventHandler OwnerChanged
	{
		add { }
		remove { }
	}

	#endregion
}

public sealed class StubResourceDictionary : IResourceDictionary
{
	#region Fields

	private readonly Dictionary<object, object> _items;
	private readonly List<IResourceProvider> _merged;
	private readonly Dictionary<ThemeVariant, IThemeVariantProvider> _themes;

	#endregion

	#region Constructors

	public StubResourceDictionary()
	{
		Calls = new StubCallLog();
		_items = new Dictionary<object, object>();
		_merged = new List<IResourceProvider>();
		_themes = new Dictionary<ThemeVariant, IThemeVariantProvider>();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	public int Count => _items.Count;

	public bool HasResources { get; set; }

	public bool IsReadOnly => false;

	public object this[object key]
	{
		get => _items[key];
		set => _items[key] = value;
	}

	public ICollection<object> Keys => _items.Keys;

	public IList<IResourceProvider> MergedDictionaries => _merged;

	public IResourceHost Owner { get; private set; }

	public IDictionary<ThemeVariant, IThemeVariantProvider> ThemeDictionaries => _themes;

	public ICollection<object> Values => _items.Values;

	#endregion

	#region Methods

	public void Add(object key, object value)
	{
		_items.Add(key, value);
	}

	public void Add(KeyValuePair<object, object> item)
	{
		_items.Add(item.Key, item.Value);
	}

	public void AddOwner(IResourceHost owner)
	{
		Calls.Add(nameof(AddOwner), owner);
		Owner = owner;
	}

	public void Clear()
	{
		_items.Clear();
	}

	public bool Contains(KeyValuePair<object, object> item)
	{
		return _items.TryGetValue(item.Key, out var value) && Equals(value, item.Value);
	}

	public bool ContainsKey(object key)
	{
		return _items.ContainsKey(key);
	}

	public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<object, object>>) _items).CopyTo(array, arrayIndex);
	}

	public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
	{
		return _items.GetEnumerator();
	}

	public bool Remove(object key)
	{
		return _items.Remove(key);
	}

	public bool Remove(KeyValuePair<object, object> item)
	{
		return Contains(item) && _items.Remove(item.Key);
	}

	public void RemoveOwner(IResourceHost owner)
	{
		if (ReferenceEquals(Owner, owner))
		{
			Owner = null;
		}
	}

	public bool TryGetResource(object key, ThemeVariant theme, out object value)
	{
		value = null;
		return false;
	}

	public bool TryGetValue(object key, out object value)
	{
		return _items.TryGetValue(key, out value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _items.GetEnumerator();
	}

	#endregion

	#region Events

	public event EventHandler OwnerChanged
	{
		add { }
		remove { }
	}

	#endregion
}

public sealed class StubXamlTypeResolver : IXamlTypeResolver
{
	#region Fields

	private readonly Dictionary<string, Type> _types;

	#endregion

	#region Constructors

	public StubXamlTypeResolver()
	{
		_types = new Dictionary<string, Type>();
	}

	#endregion

	#region Methods

	[RequiresUnreferencedCode(TrimmingMessages.XamlTypeResolvedRequiresUnreferenceCodeMessage)]
	public Type Resolve(string qualifiedTypeName)
	{
		return _types.TryGetValue(qualifiedTypeName, out var type) ? type : null;
	}

	public void SetType(string name, Type type)
	{
		_types[name] = type;
	}

	#endregion
}

public sealed class StubParentStackProvider : ICornerstoneXamlIlParentStackProvider
{
	#region Fields

	private IEnumerable<object> _parents;

	#endregion

	#region Constructors

	public StubParentStackProvider()
	{
		_parents = Array.Empty<object>();
	}

	#endregion

	#region Properties

	public IEnumerable<object> Parents
	{
		get => _parents;
		set => _parents = value ?? Array.Empty<object>();
	}

	#endregion
}

public sealed class StubTypeDescriptorContext : ITypeDescriptorContext
{
	#region Fields

	private readonly Dictionary<Type, object> _services;

	#endregion

	#region Constructors

	public StubTypeDescriptorContext()
	{
		_services = new Dictionary<Type, object>();
	}

	#endregion

	#region Properties

	public IContainer Container => null;

	public object Instance { get; set; }

	public PropertyDescriptor PropertyDescriptor { get; set; }

	#endregion

	#region Methods

	public object GetService(Type serviceType)
	{
		return _services.TryGetValue(serviceType, out var service) ? service : null;
	}

	public void OnComponentChanged()
	{
	}

	public bool OnComponentChanging()
	{
		return true;
	}

	public void SetService(Type serviceType, object service)
	{
		_services[serviceType] = service;
	}

	#endregion
}

public sealed class StubNamedAssembly : Assembly
{
	#region Fields

	private readonly AssemblyName _name;

	#endregion

	#region Constructors

	public StubNamedAssembly(string assemblyName)
	{
		FullName = assemblyName;
		_name = new AssemblyName(assemblyName);
	}

	#endregion

	#region Properties

	public override string FullName { get; }

	#endregion

	#region Methods

	public override AssemblyName GetName()
	{
		return _name;
	}

	#endregion
}

internal sealed class StubAssemblyDescriptor : IAssemblyDescriptor
{
	#region Constructors

	public StubAssemblyDescriptor(Assembly assembly)
	{
		Assembly = assembly;
		Name = assembly?.GetName()?.Name;
	}

	#endregion

	#region Properties

	public Assembly Assembly { get; }

	public Dictionary<string, IAssetDescriptor> CornerstoneResources { get; set; }

	public string Name { get; }

	public Dictionary<string, IAssetDescriptor> Resources { get; set; }

	#endregion
}

internal sealed class StubAssemblyDescriptorResolver : IAssemblyDescriptorResolver
{
	#region Fields

	private readonly Dictionary<string, IAssemblyDescriptor> _assemblies;

	#endregion

	#region Constructors

	public StubAssemblyDescriptorResolver()
	{
		_assemblies = new Dictionary<string, IAssemblyDescriptor>();
	}

	#endregion

	#region Methods

	public IAssemblyDescriptor GetAssembly(string name)
	{
		return _assemblies.TryGetValue(name, out var descriptor) ? descriptor : null;
	}

	public void InvalidateAssemblyCache()
	{
	}

	public void InvalidateAssemblyCache(string name)
	{
	}

	public void SetAssembly(string name, IAssemblyDescriptor descriptor)
	{
		_assemblies[name] = descriptor;
	}

	#endregion
}