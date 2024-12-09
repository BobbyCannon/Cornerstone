// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TypeSystem = Fody.TypeSystem;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

/// <summary>
/// A generic type system interface to decouple implementation.
/// </summary>
public interface ITypeSystem
{
	#region Properties

	/// <summary>
	/// Gets the module definition of the target module.
	/// </summary>
	ModuleDefinition ModuleDefinition { get; }

	/// <summary>
	/// Gets the fody basic type system.
	/// </summary>
	TypeSystem TypeSystem { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Finds the type in the assemblies that the weaver has registered for scanning.
	/// </summary>
	/// <param name="typeName"> Name of the type. </param>
	/// <returns> The type definition </returns>
	TypeDefinition FindType(string typeName);

	/// <summary>
	/// Finds the type in the assemblies that the weaver has registered for scanning.
	/// </summary>
	/// <param name="typeName"> Name of the type. </param>
	/// <param name="value"> The return value. </param>
	/// <returns> <c> true </c> if the type was found and value contains a valid item. </returns>
	bool TryFindType(string typeName, [NotNullWhen(true)] out TypeDefinition value);

	#endregion
}

/// <summary>
/// A <see cref="BaseModuleWeaver" /> implementing <see cref="ILogger" /> and <see cref="ITypeSystem" />, decorated with <see cref="NotNullAttribute" />.
/// </summary>
/// <seealso cref="BaseModuleWeaver" />
/// <seealso cref="FodyTools.ILogger" />
/// <seealso cref="FodyTools.ITypeSystem" />
[ExcludeFromCodeCoverage]
public abstract class AbstractModuleWeaver : BaseModuleWeaver, ILogger, ITypeSystem
{
	#region Properties

	/// <summary>
	/// The full directory path of the current weaver.
	/// </summary>
	// ReSharper disable once IdentifierTypo
	protected new string AddinDirectoryPath => base.AddinDirectoryPath;

	/// <summary>
	/// The full path of the target assembly.
	/// </summary>
	protected new string AssemblyFilePath => base.AssemblyFilePath;

	/// <summary>
	/// The full element XML from FodyWeavers.xml.
	/// </summary>
	protected new XElement Config => base.Config;

	/// <summary>
	/// A list of all the msbuild constants.
	/// A copy of the contents of the $(DefineConstants).
	/// </summary>
	protected new IList<string> DefineConstants => base.DefineConstants;

	/// <summary>
	/// An instance of <see cref="T:Mono.Cecil.ModuleDefinition" /> for processing.
	/// </summary>
	protected new ModuleDefinition ModuleDefinition => base.ModuleDefinition;

	/// <summary>
	/// The full directory path of the target project.
	/// A copy of $(ProjectDir).
	/// </summary>
	protected new string ProjectDirectoryPath => base.ProjectDirectoryPath;

	/// <summary>
	/// A list of all the references marked as copy-local.
	/// A copy of the contents of the @(ReferenceCopyLocalPaths).
	/// </summary>
	/// <remarks>
	/// This list will be actively synced back to the build system, i.e. adding or removing items from this list will modify the @(ReferenceCopyLocalPaths) list of the current build.
	/// </remarks>
	protected new IList<string> ReferenceCopyLocalPaths => base.ReferenceCopyLocalPaths;

	/// <summary>
	/// A semicolon delimited string that contains
	/// all the references for the target project.
	/// A copy of the contents of the @(ReferencePath).
	/// </summary>
	protected new string References => base.References;

	/// <summary>
	/// The full directory path of the current solution.
	/// A copy of `$(SolutionDir)` or, if it does not exist, a copy of `$(MSBuildProjectDirectory)..\..\..\`. OPTIONAL
	/// </summary>
	protected new string SolutionDirectoryPath => base.SolutionDirectoryPath;

	/// <summary>
	/// Commonly used <see cref="T:Mono.Cecil.TypeReference" />s.
	/// </summary>
	protected new TypeSystem TypeSystem => base.TypeSystem;

	#endregion

	#region Methods

	TypeDefinition ITypeSystem.FindType(string typeName)
	{
		return FindTypeDefinition(typeName);
	}

	void ILogger.LogDebug(string message)
	{
		WriteMessage(message, MessageImportance.Low);
	}

	void ILogger.LogError(string message, SequencePoint sequencePoint)
	{
		WriteError(message, sequencePoint);
	}

	void ILogger.LogError(string message, MethodReference method)
	{
		((ILogger) this).LogError(message, method.GetEntryPoint(ModuleDefinition.SymbolReader));
	}

	void ILogger.LogInfo(string message)
	{
		WriteMessage(message, MessageImportance.Normal);
	}

	void ILogger.LogWarning(string message, SequencePoint sequencePoint)
	{
		WriteWarning(message, sequencePoint);
	}

	void ILogger.LogWarning(string message, MethodReference method)
	{
		((ILogger) this).LogWarning(message, method.GetEntryPoint(ModuleDefinition.SymbolReader));
	}

	bool ITypeSystem.TryFindType(string typeName, [NotNullWhen(true)] out TypeDefinition value)
	{
		return TryFindTypeDefinition(typeName, out value);
	}

	#endregion
}