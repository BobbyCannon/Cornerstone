#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using dnlib.DotNet;

#endregion

namespace Cornerstone.VisualStudio.Core.DnlibMetadataProvider;

public class DnlibMetadataProvider : IMetadataProvider
{
	#region Methods

	public IMetadataReaderSession GetMetadata(IEnumerable<string> paths)
	{
		return new DnlibMetadataProviderSession(paths.ToArray(), true, null, null);
	}

	public IMetadataReaderSession GetMetadata(IEnumerable<string> paths, bool firstIsTarget)
	{
		return GetMetadata(paths, firstIsTarget, null);
	}

	public IMetadataReaderSession GetMetadata(IEnumerable<string> paths, bool firstIsTarget, Action<string, bool> assemblyProgress)
	{
		return GetMetadata(paths, firstIsTarget, assemblyProgress, null);
	}

	public IMetadataReaderSession GetMetadata(IEnumerable<string> paths, bool firstIsTarget, Action<string, bool> assemblyProgress, IList<string> targetPaths)
	{
		return new DnlibMetadataProviderSession(paths.ToArray(), firstIsTarget, assemblyProgress, targetPaths);
	}

	#endregion
}

internal class DnlibMetadataProviderSession : IMetadataReaderSession
{
	#region Fields

	private readonly Dictionary<ITypeDefOrRef, TypeDef> _baseTypeDefs = new();
	private readonly Dictionary<ITypeDefOrRef, ITypeDefOrRef> _baseTypes = new();
	private readonly Action<string, bool> _assemblyProgress;
	private readonly HashSet<string> _targetNames;
	private readonly ModuleContext _modCtx;

	#endregion

	#region Constructors

	public DnlibMetadataProviderSession(string[] directoryPath, bool firstIsTarget, Action<string, bool> assemblyProgress, IList<string> targetPaths)
	{
		_assemblyProgress = assemblyProgress;
		_targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var asmResolver = new AssemblyResolver
		{
			EnableTypeDefCache = false,
			UseGAC = false
		};
		var resolver = new Resolver(asmResolver)
		{
			ProjectWinMDRefs = false
		};
		_modCtx = new ModuleContext(asmResolver, resolver);
		asmResolver.DefaultModuleContext = _modCtx;

		var existing = FilterExistingAssemblyPaths(directoryPath);
		if (existing.Length == 0)
		{
			TargetAssemblyName = null;
			Assemblies = [];
		}
		else
		{
			RememberTargets(existing, firstIsTarget, targetPaths);
			TargetAssemblyName = _targetNames.Count == 0 ? null : TryGetAssemblyName(FirstTargetPath(existing, firstIsTarget, targetPaths));
			Assemblies = LoadAssemblies(_modCtx, existing, _assemblyProgress).Select(a => new AssemblyWrapper(a, this)).ToList();
		}
	}

	#endregion

	#region Properties

	public IReadOnlyCollection<IAssemblyInformation> Assemblies { get; }
	public string TargetAssemblyName { get; }

	public bool IsTargetAssembly(IAssemblyInformation assembly)
	{
		if ((assembly == null) || string.IsNullOrEmpty(assembly.Name))
		{
			return false;
		}

		return _targetNames.Contains(assembly.Name);
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		_baseTypes.Clear();
		_baseTypeDefs.Clear();
		((AssemblyResolver) _modCtx.AssemblyResolver).Clear();
	}

	public ITypeDefOrRef GetBaseType(ITypeDefOrRef type)
	{
		if (_baseTypes.TryGetValue(type, out var baseType))
		{
			return baseType;
		}
		return _baseTypes[type] = type.GetBaseType();
	}

	public TypeDef? GetTypeDef(ITypeDefOrRef type)
	{
		if (type == null)
		{
			return null;
		}

		if (type is TypeDef typeDef)
		{
			return typeDef;
		}

		if (_baseTypeDefs.TryGetValue(type, out var baseType))
		{
			return baseType;
		}
		return _baseTypeDefs[type] = type.ResolveTypeDef();
	}

	private static string[] FilterExistingAssemblyPaths(string[] paths)
	{
		if ((paths == null) || (paths.Length == 0))
		{
			return [];
		}

		var existing = new List<string>(paths.Length);
		foreach (var path in paths)
		{
			if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
			{
				existing.Add(path);
			}
		}

		return existing.ToArray();
	}

	private void RememberTargets(string[] existing, bool firstIsTarget, IList<string> targetPaths)
	{
		if ((targetPaths != null) && (targetPaths.Count > 0))
		{
			var present = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
			for (var i = 0; i < targetPaths.Count; i++)
			{
				var target = targetPaths[i];
				if (string.IsNullOrWhiteSpace(target))
				{
					continue;
				}

				string full;
				try
				{
					full = Path.GetFullPath(target);
				}
				catch (Exception)
				{
					continue;
				}

				if (!present.Contains(full))
				{
					continue;
				}

				_targetNames.Add(SimpleAssemblyName(full));
			}

			return;
		}

		if (firstIsTarget && (existing.Length > 0))
		{
			_targetNames.Add(SimpleAssemblyName(existing[0]));
		}
	}

	private static string FirstTargetPath(string[] existing, bool firstIsTarget, IList<string> targetPaths)
	{
		if ((targetPaths != null) && (targetPaths.Count > 0))
		{
			var present = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
			for (var i = 0; i < targetPaths.Count; i++)
			{
				var target = targetPaths[i];
				if (string.IsNullOrWhiteSpace(target))
				{
					continue;
				}

				try
				{
					var full = Path.GetFullPath(target);
					if (present.Contains(full))
					{
						return full;
					}
				}
				catch (Exception)
				{
				}
			}
		}

		return existing[0];
	}

	private static string SimpleAssemblyName(string path)
	{
		try
		{
			return AssemblyName.GetAssemblyName(path).Name;
		}
		catch (Exception)
		{
			return Path.GetFileNameWithoutExtension(path);
		}
	}

	private static string TryGetAssemblyName(string path)
	{
		try
		{
			return AssemblyName.GetAssemblyName(path).ToString();
		}
		catch
		{
			return Path.GetFileNameWithoutExtension(path);
		}
	}

	private static List<AssemblyDef> LoadAssemblies(ModuleContext context, string[] lst, Action<string, bool> assemblyProgress)
	{
		var asmResovler = (AssemblyResolver) context.AssemblyResolver;

		foreach (var path in lst)
		{
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory))
			{
				asmResovler.PreSearchPaths.Add(directory);
			}
		}

		var loaded = new AssemblyDef[lst.Length];
		Parallel.For(0, lst.Length, i =>
		{
			var fileName = Path.GetFileName(lst[i]);
			if (assemblyProgress != null)
			{
				assemblyProgress(fileName, true);
			}

			try
			{
				var creationOptions = new ModuleCreationOptions
				{
					TryToLoadPdbFromDisk = false
				};
				loaded[i] = AssemblyDef.Load(File.ReadAllBytes(lst[i]), creationOptions);
			}
			catch
			{
				// A single unreadable assembly does not fail the set.
			}
			finally
			{
				if (assemblyProgress != null)
				{
					assemblyProgress(fileName, false);
				}
			}
		});

		var assemblies = new List<AssemblyDef>();
		for (var i = 0; i < loaded.Length; i++)
		{
			var def = loaded[i];
			if (def == null)
			{
				continue;
			}

			var module = def.ManifestModule;
			if (module != null)
			{
				module.Context = context;
			}

			asmResovler.AddToCache(def);
			assemblies.Add(def);
		}

		return assemblies;
	}

	#endregion
}