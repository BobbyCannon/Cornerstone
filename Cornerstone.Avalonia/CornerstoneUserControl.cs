#region References

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneUserControl<T> : CornerstoneUserControl
{
	#region Constructors

	public CornerstoneUserControl() : this(null)
	{
	}

	public CornerstoneUserControl(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	public T ViewModel
	{
		get => (T) DataContext;
		set => DataContext = value;
	}

	#endregion
}

public class CornerstoneUserControl : UserControl, IDispatchable
{
	#region Fields

	private readonly IDispatcher _dispatcher;

	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	public CornerstoneUserControl() : this(CornerstoneDispatcher.Instance)
	{
	}

	public CornerstoneUserControl(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	public static object GetInstance(string type)
	{
		var response = CornerstoneApplication.GetInstance(Type.GetType(type));
		return response;
	}

	public static T GetInstance<T>()
	{
		return CornerstoneApplication.GetInstance<T>();
	}

	public virtual void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion
}