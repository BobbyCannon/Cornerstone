#region References

using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a window location.
/// </summary>
public class WindowLocation : Bindable
{
	#region Constructors

	public WindowLocation() : this(null)
	{
	}

	public WindowLocation(IDispatcher dispatcher) : base(dispatcher)
	{
		ResetToDefaults();
	}

	#endregion

	#region Properties

	public int Height { get; set; }

	public int Left { get; set; }

	public bool Maximized { get; set; }

	public int Top { get; set; }

	public int Width { get; set; }

	#endregion

	#region Methods

	public bool IsDefaultLocation()
	{
		return Maximized == false
			&& Left == -1
			&& Top == -1;
	}

	public void ResetToDefaults()
	{
		Height = 600;
		Left = -1;
		Top = -1;
		Maximized = false;
		Width = 1024;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Left}x{Top} {Height}x{Width} {Maximized}";
	}

	/// <summary>
	/// Update the WindowLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(WindowLocation update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the WindowLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(WindowLocation update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if (settings == null || settings.IsEmpty())
		{
			Height = update.Height;
			Left = update.Left;
			Top = update.Top;
			Maximized = update.Maximized;
			Width = update.Width;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Height)), x => x.Height = update.Height);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Left)), x => x.Left = update.Left);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Top)), x => x.Top = update.Top);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Maximized)), x => x.Maximized = update.Maximized);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Width)), x => x.Width = update.Width);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			WindowLocation value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}