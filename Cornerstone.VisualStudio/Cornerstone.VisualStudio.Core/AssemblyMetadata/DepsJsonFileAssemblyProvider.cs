#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public class DepsJsonFileAssemblyProvider : IAssemblyProvider
{
	#region Fields

	private readonly string _path;
	private readonly string _xamlPrimaryAssemblyPath;

	#endregion

	#region Constructors

	public DepsJsonFileAssemblyProvider(string executablePath, string xamlPrimaryAssemblyPath)
	{
		if (string.IsNullOrEmpty(executablePath))
		{
			throw new ArgumentNullException(nameof(executablePath));
		}
		_path = executablePath;
		_xamlPrimaryAssemblyPath = xamlPrimaryAssemblyPath;
	}

	#endregion

	#region Methods

	public IEnumerable<string> GetAssemblies()
	{
		var result = new List<string>(300);
		if (!string.IsNullOrEmpty(_xamlPrimaryAssemblyPath))
		{
			result.Add(_xamlPrimaryAssemblyPath);
		}
		try
		{
			result.AddRange(GetAssemblies(_path));
		}
		catch (Exception ex) when
			(ex is DirectoryNotFoundException 
				or FileNotFoundException)
		{
		}
		catch (Exception ex)
		{
			throw new IOException($"Failed to read file '{_path}'.", ex);
		}
		return result;
	}

	private static IEnumerable<string> GetAssemblies(string path)
	{
		if (Path.GetDirectoryName(path) is not { } directory)
		{
			return [];
		}

		var depsPath = Path.Combine(directory,
			Path.GetFileNameWithoutExtension(path) + ".deps.json");
		if (File.Exists(depsPath))
		{
			return DepsJsonAssemblyListLoader.ParseFile(depsPath);
		}
		return Directory.GetFiles(directory).Where(f => f.EndsWith(".dll") || f.EndsWith(".exe"));
	}

	#endregion
}