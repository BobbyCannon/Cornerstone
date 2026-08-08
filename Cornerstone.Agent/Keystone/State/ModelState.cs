#region References

using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Agent.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
[DependencyInjected]
public partial class ModelState : CornerstoneObject
{
	#region Constructors

	public ModelState()
	{
		Models = new SpeedyList<ModelInfo>();
		ModelIngress = new TextIngress();
		OutputIngress = new TextIngress();
		RawIngress = new TextIngress();
		ReasoningIngress = new TextIngress();
		SystemIngress = new TextIngress();
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when a prompt is being processed (load and/or inference).
	/// Blocks model selection while set.
	/// </summary>
	public partial bool IsExecuting { get; set; }

	/// <summary>
	/// True while LLama weights are loading into memory.
	/// </summary>
	public partial bool IsModelLoading { get; set; }

	public partial bool IsRefreshingModels { get; set; }

	/// <summary>
	/// Path of the model currently loaded in memory; null if none.
	/// </summary>
	public partial string LoadedModelPath { get; set; }

	public partial double LoadingPercent { get; set; }

	public TextIngress ModelIngress { get; set; }

	public SpeedyList<ModelInfo> Models { get; }

	public TextIngress OutputIngress { get; set; }

	public TextIngress RawIngress { get; set; }

	public TextIngress ReasoningIngress { get; set; }

	public partial string RefreshStatusMessage { get; set; }

	/// <summary>
	/// Desired model path (user selection). Not loaded until the next request.
	/// Kept in sync with <see cref="AppSettings.SelectedModel"/>.
	/// </summary>
	public partial string SelectedModelPath { get; set; }

	public TextIngress SystemIngress { get; set; }

	/// <summary>
	/// True when the user has selected a different model than what is loaded.
	/// </summary>
	public bool HasPendingModelSwitch =>
		!string.IsNullOrEmpty(SelectedModelPath)
		&& !string.Equals(SelectedModelPath, LoadedModelPath, System.StringComparison.OrdinalIgnoreCase);

	#endregion
}
