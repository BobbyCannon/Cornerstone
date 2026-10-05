#region References

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

internal static class ModuleInitializer
{
	#region Methods

	[ModuleInitializer]
	internal static void TestInit()
	{
		Trace.Listeners.Insert(0, new ThrowListener());
	}

	#endregion

	#region Classes

	private class ThrowListener : TextWriterTraceListener
	{
		#region Methods

		public override void Fail(string message)
		{
			throw new Exception("Assertion Failed. " + message);
		}

		public override void Fail(string message, string detailMessage)
		{
			throw new Exception("Assertion Failed. " + message + detailMessage);
		}

		#endregion
	}

	#endregion
}