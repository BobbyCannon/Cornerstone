﻿#region References

using Cornerstone.Maui;
using Microsoft.Maui.Hosting;

#endregion

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// ReSharper disable once CheckNamespace
namespace Sample.Client.Maui.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
	#region Constructors

	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		InitializeComponent();

		MauiPlatform.Initialize();
	}

	#endregion

	#region Methods

	protected override MauiApp CreateMauiApp()
	{
		return MauiProgram.CreateMauiApp();
	}

	#endregion
}