namespace Cornerstone.Threading;

public class ReaderWriterLockStub : IReaderWriterLock
{
	#region Properties

	public bool IsAwaitingWriteLock { get; set; }
	public bool IsReadLockHeld { get; set; }
	public bool IsWriteLockHeld { get; set; }

	#endregion

	#region Methods

	public void EnterReadLock()
	{
	}

	public void EnterUpgradeableReadLock()
	{
	}

	public void EnterWriteLock()
	{
	}

	public void ExitReadLock()
	{
	}

	public void ExitUpgradeableReadLock()
	{
	}

	public void ExitWriteLock()
	{
	}

	#endregion
}