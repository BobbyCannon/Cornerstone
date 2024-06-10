#region References

using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Windows;



/// <summary>
/// Represents a window location.
/// </summary>
public class WindowLocation : Notifiable
{
	#region Constructors

	/// <inheritdoc />
	public WindowLocation()
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
		return (Maximized == false)
			&& (Left == -1)
			&& (Top == -1);
	}

	public void ResetToDefaults()
	{
		Height = 600;
		Left = -1;
		Top = -1;
		Maximized = false;
		Width = 1024;
	}

	/// <summary>
	/// Update the WindowLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(WindowLocation update)
	{
		return UpdateWith(update, UpdateableOptions.Empty);
	}

	/// <summary>
	/// Update the WindowLocation with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(WindowLocation update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			Height = update.Height;
			Left = update.Left;
			Top = update.Top;
			Maximized = update.Maximized;
			Width = update.Width;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Height)), x => x.Height = update.Height);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Left)), x => x.Left = update.Left);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Top)), x => x.Top = update.Top);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Maximized)), x => x.Maximized = update.Maximized);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Width)), x => x.Width = update.Width);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			WindowLocation value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}