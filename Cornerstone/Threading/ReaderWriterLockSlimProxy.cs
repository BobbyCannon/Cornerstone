#region References

using System.Threading;

#endregion

namespace Cornerstone.Threading;

/// <inheritdoc cref="IReaderWriterLock" />
public class ReaderWriterLockSlimProxy : ReaderWriterLockSlim, IReaderWriterLock
{
	#region Constructors

	public ReaderWriterLockSlimProxy() : base(LockRecursionPolicy.SupportsRecursion)
	{
	}

	#endregion

	#region Properties

	
	public bool IsAwaitingWriteLock => WaitingWriteCount > 0;

	#endregion
}