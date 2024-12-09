#region References

using System;

#endregion

namespace Sample.Shared.Build;

public class SampleEventSourceEventArgs : EventArgs
{
	#region Constructors

	public SampleEventSourceEventArgs(bool cancel)
	{
		Cancel = cancel;
	}

	#endregion

	#region Properties

	public bool Cancel { get; }

	#endregion
}