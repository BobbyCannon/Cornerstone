﻿#region References

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

	public CornerstoneUserControl() : this(default, null)
	{
	}

	public CornerstoneUserControl(IDispatcher dispatcher) : this(default, dispatcher)
	{
	}

	public CornerstoneUserControl(T viewModel, IDispatcher dispatcher) : base(dispatcher)
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

	public static object GetService(string type)
	{
		var response = CornerstoneApplication.GetService(Type.GetType(type));
		return response;
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