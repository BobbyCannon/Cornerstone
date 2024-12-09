#region References

using System.Collections.Generic;
using Cornerstone.Weaver.Fody.FodyTools;
using Cornerstone.Weaver.Fody.PropertyChanged;
using Cornerstone.Weaver.Fody.PropertyChanging;

#endregion

namespace Cornerstone.Weaver.Fody;

/// <summary>
/// </summary>
/// <remarks>
/// https://github.com/Fody/Home/blob/master/pages/addin-development.md
/// </remarks>
public class ModuleWeaver : AbstractModuleWeaver
{
	#region Properties

	public override bool ShouldCleanReference => true;
	
	public IEnumerable<CornerstoneWeaver> Weavers { get; private set; }

	#endregion

	#region Methods

	public override void Execute()
	{
		// un-comment to connect debugger
		//System.Diagnostics.Debugger.Launch();

		//WriteMessage("Hello Build!", MessageImportance.High);

		Weavers =
		[
			new WeaverForPropertyChanged(this),
			new WeaverForPropertyChanging(this),
		];

		foreach (var weaver in Weavers)
		{
			weaver.Weave();
		}
	}

	/// <inheritdoc />
	public override IEnumerable<string> GetAssembliesForScanning()
	{
		var response = new List<string>();
		response.AddRange(WeaverForPropertyChanged.GetAssembliesForScanning());
		response.AddRange(WeaverForPropertyChanging.GetAssembliesForScanning());
		return response;
	}

	#endregion
}