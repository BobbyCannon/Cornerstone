#region References

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using dnlib.DotNet;

#endregion

namespace Cornerstone.VisualStudio.Core.DnlibMetadataProvider;

public class DnlibMetadataProvider : IMetadataProvider
{
	#region Methods

	public IMetadataReaderSession GetMetadata(IEnumerable<string> paths)
	{
		return new DnlibMetadataProviderSession(paths.ToArray());
	}

	#endregion
}

internal class DnlibMetadataProviderSession : IMetadataReaderSession
{
	#region Fields

	private readonly Dictionary<ITypeDefOrRef, TypeDef> _baseTypeDefs = new();
	private readonly Dictionary<ITypeDefOrRef, ITypeDefOrRef> _baseTypes = new();
	private readonly ModuleContext _modCtx;

	#endregion

	#region Constructors

	public DnlibMetadataProviderSession(string[] directoryPath)
	{
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

		if ((directoryPath == null) || (directoryPath.Length == 0))
		{
			TargetAssemblyName = null;
			Assemblies = [];
		}
		else
		{
			TargetAssemblyName = AssemblyName.GetAssemblyName(directoryPath[0]).ToString();
			Assemblies = LoadAssemblies(_modCtx, directoryPath).Select(a => new AssemblyWrapper(a, this)).ToList();
		}
	}

	#endregion

	#region Properties

	public IReadOnlyCollection<IAssemblyInformation> Assemblies { get; }
	public string? TargetAssemblyName { get; }

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

	private static List<AssemblyDef> LoadAssemblies(ModuleContext context, string[] lst)
	{
		var asmResovler = (AssemblyResolver) context.AssemblyResolver;

		foreach (var path in lst)
		{
			asmResovler.PreSearchPaths.Add(path);
		}

		var assemblies = new List<AssemblyDef>();

		foreach (var asm in lst)
		{
			try
			{
				var creationOptions = new ModuleCreationOptions(context)
				{
					TryToLoadPdbFromDisk = false
				};
				var def = AssemblyDef.Load(File.ReadAllBytes(asm), creationOptions);
				asmResovler.AddToCache(def);
				assemblies.Add(def);
			}
			catch
			{
				//Ignore
			}
		}

		return assemblies;
	}

	#endregion
}