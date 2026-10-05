#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

/// <summary>
/// A <see cref="TextBox" /> which stores the latest binding error state.
/// </summary>
public class ErrorCollectingTextBox : TextBox
{
	#region Properties

	public Exception Error { get; private set; }
	public BindingValueType ErrorState { get; private set; }

	#endregion

	#region Methods

	protected override void UpdateDataValidation(PresentationProperty property, BindingValueType state, Exception error)
	{
		if (property == TextProperty)
		{
			Error = error;
			ErrorState = state;
		}

		base.UpdateDataValidation(property, state, error);
	}

	#endregion
}