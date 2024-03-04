#region References

using System;
using Sample.Client.Wpf.Windows;

#endregion

namespace Sample.Client.Wpf;

public static class Program
{
	#region Methods

	[STAThread]
	[LoaderOptimization(LoaderOptimization.MultiDomainHost)]
	public static int Main(string[] args)
	{
		var application = new App();
		application.InitializeComponent();
		return application.Run();
	}

	#endregion
}