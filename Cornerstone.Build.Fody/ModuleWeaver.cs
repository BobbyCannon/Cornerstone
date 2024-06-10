#region References

using System.Collections.Generic;
using Fody;

#endregion

namespace Cornerstone.Build.Fody;

public class ModuleWeaver : BaseModuleWeaver
{
	#region Properties

	public override bool ShouldCleanReference => true;

	#endregion

	#region Methods

	public override void Execute()
	{
		// un-comment to connect debugger
		//System.Diagnostics.Debugger.Launch();

		//throw new WeavingException("Booo");
	}

	/// <inheritdoc />
	public override IEnumerable<string> GetAssembliesForScanning()
	{
		yield return "mscorlib";
		yield return "System";
		yield return "System.Runtime";
		yield return "System.Core";
		yield return "netstandard";
		yield return "System.Collections";
		yield return "System.ObjectModel";
		yield return "System.Threading";
		yield return "FSharp.Core";
		//TODO: remove when move to only netstandard2.0
		yield return "System.Diagnostics.Tools";
		yield return "System.Diagnostics.Debug";
	}

	#endregion
}