#region References

using System;
using Cornerstone.Presentation.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class TestWithServicesBase : IDisposable
{
	#region Fields

	private readonly IDisposable _scope;

	#endregion

	#region Constructors

	public TestWithServicesBase()
	{
		_scope = PresentationLocator.EnterScope();
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		if (Dispatcher.UIThread.CheckAccess())
		{
			Dispatcher.UIThread.RunJobs();
		}

		_scope.Dispose();
	}

	#endregion
}