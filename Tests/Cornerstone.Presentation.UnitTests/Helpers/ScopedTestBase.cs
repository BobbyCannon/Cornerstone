#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class ScopedTestBase : IDisposable
{
	#region Fields

	private readonly IDisposable _scope;

	#endregion

	#region Constructors

	public ScopedTestBase()
	{
		PresentationLocator.Current = PresentationLocator.CurrentMutable = new PresentationLocator();
		Dispatcher.ResetBeforeUnitTests();
		Control.ResetLoadedQueueForUnitTests();
		_scope = PresentationLocator.EnterScope();
	}

	#endregion

	#region Methods

	public virtual void Dispose()
	{
		Dispatcher.ResetForUnitTests();
		_scope.Dispose();
	}

	#endregion
}