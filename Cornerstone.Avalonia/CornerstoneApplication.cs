﻿#region References

using System;
using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Exceptions;
using Cornerstone.Input;
using Cornerstone.Media;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Avalonia;

public abstract class CornerstoneApplication : Application, IDispatchable
{
	#region Fields

	private static CornerstoneDispatcher _dispatcher;
	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	protected CornerstoneApplication()
	{
		ApplicationArguments = new ApplicationArguments();
		RuntimeInformation.SetApplicationAssembly(Assembly.GetCallingAssembly());
		RuntimeInformation.Refresh();
	}

	static CornerstoneApplication()
	{
		DependencyProvider = new DependencyProvider("Cornerstone");
		RuntimeInformation = new();
	}

	#endregion

	#region Properties

	public ApplicationArguments ApplicationArguments { get; }

	public static DependencyProvider DependencyProvider { get; }

	public static CornerstoneDispatcher Dispatcher => _dispatcher ??= new CornerstoneDispatcher();

	public static RuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IDispatcher GetDispatcher()
	{
		return Dispatcher;
	}

	public static object GetService(string type)
	{
		var response = GetService(Type.GetType(type));
		return response;
	}

	public static T GetService<T>()
	{
		var app = (CornerstoneApplication) Current;
		if (app != null)
		{
			return DependencyProvider.GetInstance<T>();
		}

		throw new CornerstoneException("Application is not available.");
	}

	public static object GetService(Type type)
	{
		var app = (CornerstoneApplication) Current;
		if (app != null)
		{
			return DependencyProvider.GetInstance(type);
		}

		throw new CornerstoneException("Application is not available.");
	}

	/// <inheritdoc />
	public override void OnFrameworkInitializationCompleted()
	{
		switch (ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime sValue:
			{
				ApplicationArguments.Parse(sValue.Args);
				break;
			}
			case ISingleViewApplicationLifetime sValue:
			{
				ApplicationArguments.Parse(sValue.GetBrowserArgs());
				break;
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	public void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= this.GetPropertyChangedHandler();
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <inheritdoc />
	public override void RegisterServices()
	{
		DependencyProvider.AddSingleton(ApplicationArguments);
		DependencyProvider.AddSingleton<AudioPlayer, AudioPlayerStub>();
		DependencyProvider.AddSingleton<IBrowserProxy, BrowserProxy>();
		DependencyProvider.AddSingleton<IClipboardService, ClipboardService>();
		DependencyProvider.AddSingleton<Keyboard, KeyboardStub>();
		DependencyProvider.AddSingleton<Mouse, MouseStub>();

		DependencyProvider.AddCornerstoneServices(RuntimeInformation, Dispatcher);
		DependencyProvider.Lock();
		base.RegisterServices();
	}

	public static bool TryGetService<T>(out T service)
	{
		var app = (CornerstoneApplication) Current;
		if (app != null)
		{
			service = DependencyProvider.GetInstance<T>();
			return true;
		}

		service = default;
		return false;
	}

	#endregion
}