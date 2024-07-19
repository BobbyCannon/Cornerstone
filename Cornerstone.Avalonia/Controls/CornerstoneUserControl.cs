#region References

using System.ComponentModel;
using Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class CornerstoneUserControl<T> : CornerstoneUserControl
{
	#region Constructors

	public CornerstoneUserControl() : this(default)
	{
	}

	public CornerstoneUserControl(T viewModel)
	{
		ViewModel = viewModel;
	}

	#endregion

	#region Properties

	public T ViewModel
	{
		get => (T) DataContext;
		protected set => DataContext = value;
	}

	#endregion
}

public class CornerstoneUserControl : UserControl, IDispatchable
{
	#region Fields

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Methods

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return CornerstoneDispatcher.Instance;
	}

	public static T GetService<T>()
	{
		return CornerstoneApplication.GetService<T>();
	}

	public virtual void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}