#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Input;

public class ExpectedKeyState : Bindable<ExpectedKeyState>
{
	#region Constructors

	public ExpectedKeyState()
	{
		TriggerOnPress = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets a value indicating if either the left or right alt key is required.
	/// </summary>
	public bool IsAltRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if either the left or right control key is required.
	/// </summary>
	public bool IsControlRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if the left alt key is required.
	/// </summary>
	public bool IsLeftAltRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if the left control key is required.
	/// </summary>
	public bool IsLeftControlRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if the left shift key is required.
	/// </summary>
	public bool IsLeftShiftRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if the right alt key is required.
	/// </summary>
	public bool IsRightAltRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if the right control key is required.
	/// </summary>
	public bool IsRightControlRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if the right shift key is required.
	/// </summary>
	public bool IsRightShiftRequired { get; set; }

	/// <summary>
	/// Gets a value indicating if either the left or right shift key is required.
	/// </summary>
	public bool IsShiftRequired { get; set; }

	/// <summary>
	/// Gets a value of the key being changed (up or down).
	/// </summary>
	public KeyboardKey Key { get; set; }

	/// <summary>
	/// The state is expected to happen on key release.
	/// </summary>
	public bool TriggerOnPress { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the ExpectedKeyState with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(ExpectedKeyState update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			IsAltRequired = update.IsAltRequired;
			IsControlRequired = update.IsControlRequired;
			IsLeftAltRequired = update.IsLeftAltRequired;
			IsLeftControlRequired = update.IsLeftControlRequired;
			IsLeftShiftRequired = update.IsLeftShiftRequired;
			IsRightAltRequired = update.IsRightAltRequired;
			IsRightControlRequired = update.IsRightControlRequired;
			IsRightShiftRequired = update.IsRightShiftRequired;
			IsShiftRequired = update.IsShiftRequired;
			Key = update.Key;
			TriggerOnPress = update.TriggerOnPress;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsAltRequired)), x => x.IsAltRequired = update.IsAltRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsControlRequired)), x => x.IsControlRequired = update.IsControlRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsLeftAltRequired)), x => x.IsLeftAltRequired = update.IsLeftAltRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsLeftControlRequired)), x => x.IsLeftControlRequired = update.IsLeftControlRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsLeftShiftRequired)), x => x.IsLeftShiftRequired = update.IsLeftShiftRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsRightAltRequired)), x => x.IsRightAltRequired = update.IsRightAltRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsRightControlRequired)), x => x.IsRightControlRequired = update.IsRightControlRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsRightShiftRequired)), x => x.IsRightShiftRequired = update.IsRightShiftRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IsShiftRequired)), x => x.IsShiftRequired = update.IsShiftRequired);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Key)), x => x.Key = update.Key);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(TriggerOnPress)), x => x.TriggerOnPress = update.TriggerOnPress);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			ExpectedKeyState value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}