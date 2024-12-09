#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Profiling;

public class ProgressTracker : Bindable<ProgressTracker>
{
	#region Constructors

	public ProgressTracker() : this(null)
	{
	}

	public ProgressTracker(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	public string DisplayLabel => $"{Percent:F0}%";

	public bool IsProgressing { get; set; }

	public int Maximum { get; private set; }

	public int Minimum { get; private set; }

	public decimal Percent { get; private set; }

	public string Remaining { get; set; }

	public int SecondsRemaining { get; set; }

	public string Status { get; set; }

	public int Value { get; private set; }

	#endregion

	#region Methods

	public void Stop()
	{
		IsProgressing = false;
	}

	public void Update(int value)
	{
		Value = value;
		Percent = ((value * 1.0m) / (Maximum - Minimum)) * 100;
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

	/// <summary>
	/// Update the ProgressDetails with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(ProgressTracker update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			IsProgressing = update.IsProgressing;
			Maximum = update.Maximum;
			Minimum = update.Minimum;
			Percent = update.Percent;
			Remaining = update.Remaining;
			SecondsRemaining = update.SecondsRemaining;
			Status = update.Status;
			Value = update.Value;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsProgressing)), x => x.IsProgressing = update.IsProgressing);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Maximum)), x => x.Maximum = update.Maximum);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Minimum)), x => x.Minimum = update.Minimum);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Percent)), x => x.Percent = update.Percent);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Remaining)), x => x.Remaining = update.Remaining);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SecondsRemaining)), x => x.SecondsRemaining = update.SecondsRemaining);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Status)), x => x.Status = update.Status);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Value)), x => x.Value = update.Value);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			ProgressTracker value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}