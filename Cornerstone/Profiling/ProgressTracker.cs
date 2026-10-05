#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Tracks percent-complete style progress for UI (status, remaining, bar bounds).
/// </summary>
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[SourceReflection]
public partial class ProgressTracker : CornerstoneObject<ProgressTracker>
{
	#region Constructors

	public ProgressTracker()
	{
		Stop();
	}

	#endregion

	#region Properties

	public string DisplayLabel => $"{Percent:F0}%";

	public bool IsProgressing => Percent is >= 0 and <= 100;

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Maximum { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Minimum { get; set; }

	[Notify]
	[AlsoNotify(nameof(IsProgressing), nameof(DisplayLabel))]
	[UpdateableAction(UpdateableAction.All)]
	public partial decimal Percent { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Remaining { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int SecondsRemaining { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Status { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Value { get; set; }

	#endregion

	#region Methods

	public void Stop()
	{
		Update(-1, 0, 100);
	}

	public void Update(int value)
	{
		Value = value;

		var length = Maximum - Minimum;
		Percent = length <= 0 ? 0 : ((value * 1.0m) / length) * 100;
	}

	public void Update(int value, int minimum, int maximum)
	{
		Minimum = minimum;
		Maximum = maximum;

		Update(value);
	}

	public void UpdatePercent(int percent)
	{
		Percent = percent;
		Value = percent >= 0 ? percent : 0;
		Minimum = 0;
		Maximum = 100;
	}

	#endregion
}
