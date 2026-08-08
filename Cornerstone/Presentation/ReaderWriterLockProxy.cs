#region References

using Cornerstone.Reflection;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a proxy for a reader writer lock.
/// </summary>
[SourceReflection]
public class ReaderWriterLockProxy : CornerstoneObject, IReaderWriterLock
{
	#region Fields

	private readonly IReaderWriterLock _readerWriterLock;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the bindable object.
	/// </summary>
	/// <param name="readerWriterLock"> An optional lock. Defaults to <see cref="ReaderWriterLockTiny" /> if not provided. </param>
	public ReaderWriterLockProxy(IReaderWriterLock readerWriterLock = null)
	{
		_readerWriterLock = readerWriterLock ?? new ReaderWriterLockTiny();
	}

	#endregion

	#region Properties

	public bool IsAwaitingWriteLock => _readerWriterLock.IsAwaitingWriteLock;

	public bool IsReadLockHeld => _readerWriterLock.IsReadLockHeld;

	public bool IsWriteLockHeld => _readerWriterLock.IsWriteLockHeld;

	#endregion

	#region Methods

	public void EnterReadLock()
	{
		_readerWriterLock.EnterReadLock();
	}

	public void EnterUpgradeableReadLock()
	{
		_readerWriterLock.EnterUpgradeableReadLock();
	}

	public void EnterWriteLock()
	{
		_readerWriterLock.EnterWriteLock();
	}

	public void ExitReadLock()
	{
		_readerWriterLock.ExitReadLock();
	}

	public void ExitUpgradeableReadLock()
	{
		_readerWriterLock.ExitUpgradeableReadLock();
	}

	public void ExitWriteLock()
	{
		_readerWriterLock.ExitWriteLock();
	}

	#endregion
}