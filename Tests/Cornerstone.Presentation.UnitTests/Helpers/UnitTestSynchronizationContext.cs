#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public sealed class UnitTestSynchronizationContext : SynchronizationContext
{
	#region Fields

	private readonly List<(SendOrPostCallback callback, object state)> _postedCallbacks = [];

	#endregion

	#region Methods

	public static Scope Begin()
	{
		var sync = new UnitTestSynchronizationContext();
		var old = Current;
		SetSynchronizationContext(sync);
		return new Scope(old, sync);
	}

	public void ExecutePostedCallbacks()
	{
		lock (_postedCallbacks)
		{
			_postedCallbacks.ForEach(t => t.callback(t.state));
			_postedCallbacks.Clear();
		}
	}

	public override void Post(SendOrPostCallback d, object state)
	{
		lock (_postedCallbacks)
		{
			_postedCallbacks.Add((d, state));
		}
	}

	public override void Send(SendOrPostCallback d, object state)
	{
		d(state);
	}

	#endregion

	#region Classes

	public class Scope : IDisposable
	{
		#region Fields

		private readonly UnitTestSynchronizationContext _new;
		private readonly SynchronizationContext _old;

		#endregion

		#region Constructors

		public Scope(SynchronizationContext old, UnitTestSynchronizationContext n)
		{
			_old = old;
			_new = n;
		}

		#endregion

		#region Methods

		public void Dispose()
		{
			SetSynchronizationContext(_old);
		}

		public void ExecutePostedCallbacks()
		{
			_new.ExecutePostedCallbacks();
		}

		#endregion
	}

	#endregion
}