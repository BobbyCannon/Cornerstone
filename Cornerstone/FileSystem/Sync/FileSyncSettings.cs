namespace Cornerstone.FileSystem.Sync;

public class FileSyncSettings
{
	#region Constructors

	public FileSyncSettings()
	{
		CopyEmptyDirectories = true;
		CopyLeftOnlyFiles = true;
		DeleteRightOnlyDirectories = true;
		DeleteRightOnlyFiles = true;
		Pattern = "*";
		Recursive = true;
		UpdateChangedFiles = true;
	}

	#endregion

	#region Properties

	public bool CopyEmptyDirectories { get; set; }

	public bool CopyLeftOnlyFiles { get; set; }

	public bool DeleteRightOnlyDirectories { get; set; }

	public bool DeleteRightOnlyFiles { get; set; }

	public string Pattern { get; set; }

	public bool Quiet { get; set; }

	public bool Recursive { get; set; }

	public bool UpdateChangedFiles { get; private set; }

	public bool WhatIf { get; set; }

	#endregion
}