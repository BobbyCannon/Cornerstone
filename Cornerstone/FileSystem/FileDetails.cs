#region References

using System;

#endregion

namespace Cornerstone.FileSystem;

public class FileDetails
{
	#region Properties

	public string DisplayPath { get; set; }

	public string Extension { get; set; }

	/// <summary>
	/// Important: Identifier that works across platforms
	/// On desktop: full file path (string)
	/// On Android: URI string (e.g., content://...)
	/// </summary>
	public string Identifier { get; set; }

	public DateTimeOffset LastModified { get; set; }

	public long Length { get; set; }

	public string Name { get; set; }

	#endregion
}