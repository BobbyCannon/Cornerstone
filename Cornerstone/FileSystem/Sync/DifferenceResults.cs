#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.FileSystem.Sync;

public class DifferenceResults
{
	#region Constructors

	public DifferenceResults(
		ICollection<string> changedFiles,
		ICollection<string> identicalFiles,
		ICollection<string> leftFiles,
		ICollection<string> leftOnlyFiles,
		ICollection<string> leftOnlyFolders,
		ICollection<string> rightFiles,
		ICollection<string> rightOnlyFiles,
		ICollection<string> rightOnlyFolders)
	{
		ChangedFiles = changedFiles;
		IdenticalFiles = identicalFiles;
		LeftFiles = leftFiles;
		LeftOnlyFiles = leftOnlyFiles;
		LeftOnlyFolders = leftOnlyFolders;
		RightFiles = rightFiles;
		RightOnlyFiles = rightOnlyFiles;
		RightOnlyFolders = rightOnlyFolders;

		Log = new SyncLog();
	}

	#endregion

	#region Properties

	public bool AreFullyIdentical =>
		!ChangedFiles.Any() &&
		!LeftOnlyFiles.Any() &&
		!LeftOnlyFolders.Any() &&
		!RightOnlyFiles.Any() &&
		!RightOnlyFolders.Any();

	public ICollection<string> ChangedFiles { get; }

	public ICollection<string> IdenticalFiles { get; }

	public ICollection<string> LeftFiles { get; }

	public ICollection<string> LeftOnlyFiles { get; }

	public ICollection<string> LeftOnlyFolders { get; }

	public SyncLog Log { get; }

	public ICollection<string> RightFiles { get; }

	public ICollection<string> RightOnlyFiles { get; }

	public ICollection<string> RightOnlyFolders { get; }

	#endregion
}