#region References

using Cornerstone.Threading;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a bindable that is also lockable (thread-safe).
/// </summary>
public class ReaderWriterLockBindable : Bindable, IReaderWriterLock
{
	#region Fields

	private readonly IReaderWriterLock _readerWriterLock;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the bindable object.
	/// </summary>
	/// <param name="readerWriterLock"> An optional lock. Defaults to <see cref="ReaderWriterLockTiny" /> if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public ReaderWriterLockBindable(IReaderWriterLock readerWriterLock = null, IDispatcher dispatcher = null) : base(dispatcher)
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