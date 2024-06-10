#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Provides ITextSourceVersion instances.
/// </summary>
public class TextSourceVersionProvider
{
	#region Fields

	private Version _currentVersion;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new TextSourceVersionProvider instance.
	/// </summary>
	public TextSourceVersionProvider()
	{
		_currentVersion = new Version(this);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the current version.
	/// </summary>
	public ITextSourceVersion CurrentVersion => _currentVersion;

	#endregion

	#region Methods

	/// <summary>
	/// Replaces the current version with a new version.
	/// </summary>
	/// <param name="change"> Change from current version to new version </param>
	public void AppendChange(TextChangeEventArgs change)
	{
		_currentVersion.Change = change ?? throw new ArgumentNullException(nameof(change));
		_currentVersion.Next = new Version(_currentVersion);
		_currentVersion = _currentVersion.Next;
	}

	#endregion

	#region Classes

	[DebuggerDisplay("Version #{" + nameof(_id) + "}")]
	private sealed class Version : ITextSourceVersion
	{
		#region Fields

		// the change from this version to the next version
		internal TextChangeEventArgs Change;

		internal Version Next;

		// ID used for CompareAge()
		private readonly int _id;

		// Reference back to the provider.
		// Used to determine if two checkpoints belong to the same document.
		private readonly TextSourceVersionProvider _provider;

		#endregion

		#region Constructors

		internal Version(TextSourceVersionProvider provider)
		{
			_provider = provider;
		}

		internal Version(Version prev)
		{
			_provider = prev._provider;
			_id = unchecked(prev._id + 1);
		}

		#endregion

		#region Methods

		public bool BelongsToSameDocumentAs(ITextSourceVersion other)
		{
			var o = other as Version;
			return (o != null) && (_provider == o._provider);
		}

		public int CompareAge(ITextSourceVersion other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other));
			}
			var o = other as Version;
			if ((o == null) || (_provider != o._provider))
			{
				throw new ArgumentException("Versions do not belong to the same document.");
			}
			// We will allow overflows, but assume that the maximum distance between checkpoints is 2^31-1.
			// This is guaranteed on x86 because so many checkpoints don't fit into memory.
			return Math.Sign(unchecked(_id - o._id));
		}

		public IEnumerable<TextChangeEventArgs> GetChangesTo(ITextSourceVersion other)
		{
			var result = CompareAge(other);
			var o = (Version) other;
			if (result < 0)
			{
				return GetForwardChanges(o);
			}
			if (result > 0)
			{
				return o.GetForwardChanges(this).Reverse().Select(c => c.Invert());
			}
			return [];
		}

		public int MoveOffsetTo(ITextSourceVersion other, int oldOffset, AnchorMovementType movement)
		{
			return GetChangesTo(other)
				.Aggregate(oldOffset, (current, e) => e.GetNewOffset(current, movement));
		}

		private IEnumerable<TextChangeEventArgs> GetForwardChanges(Version other)
		{
			// Return changes from this(inclusive) to other(exclusive).
			for (var node = this; node != other; node = node.Next)
			{
				yield return node.Change;
			}
		}

		#endregion
	}

	#endregion
}