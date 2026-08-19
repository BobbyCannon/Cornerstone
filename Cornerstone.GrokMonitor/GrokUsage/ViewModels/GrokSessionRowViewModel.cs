#region References

using System;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.ViewModels;

/// <summary>
/// Presentation row for one session in the usage grid (not a State type).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class GrokSessionRowViewModel : ViewModel, IGrokSessionUsage, IUpdateable<IGrokSessionUsage>
{
	#region Constructors

	public GrokSessionRowViewModel()
	{
		SessionId = string.Empty;
		Title = string.Empty;
		WorkingDirectory = string.Empty;
		CurrentModelId = string.Empty;
		EventsPath = string.Empty;
		SessionDirectory = string.Empty;
		SummaryPath = string.Empty;
		LastInferenceAtStr = string.Empty;
	}

	#endregion

	#region Properties

	[Notify]
	public partial long CachedPromptTokens { get; set; }

	[Notify]
	public partial long CompletionTokens { get; set; }

	[Notify]
	public partial string CurrentModelId { get; set; }

	[Notify]
	public partial string EventsPath { get; set; }

	[Notify]
	public partial DateTimeOffset FirstInferenceAt { get; set; }

	[Notify]
	public partial bool HasAllocatedUsage { get; set; }

	[Notify]
	public partial int InferenceCount { get; set; }

	[Notify]
	public partial DateTimeOffset LastInferenceAt { get; set; }

	/// <summary>
	/// Display-only last inference time (empty when unknown).
	/// </summary>
	[Notify]
	public partial string LastInferenceAtStr { get; set; }

	[Notify]
	public partial int MessageCount { get; set; }

	[Notify]
	public partial long PromptTokens { get; set; }

	[Notify]
	public partial long ReasoningTokens { get; set; }

	[Notify]
	public partial string SessionDirectory { get; set; }

	[Notify]
	public partial string SessionId { get; set; }

	[Notify]
	public partial string SummaryPath { get; set; }

	[Notify]
	public partial string Title { get; set; }

	[Notify]
	public partial long TotalTokens { get; set; }

	[Notify]
	public partial double UsagePercent { get; set; }

	[Notify]
	public partial string WorkingDirectory { get; set; }

	#endregion

	#region Methods

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if (propertyName == nameof(LastInferenceAt))
		{
			LastInferenceAtStr = LastInferenceAt == default
				? string.Empty
				: LastInferenceAt.ToString("u");
		}
	}

	#endregion
}