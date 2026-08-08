#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;

#endregion

namespace Cornerstone.VisualStudio.SuggestedActions.Actions.Base;

internal class BaseSuggestedAction
{
	#region Properties

	public bool HasActionSets { get; }

	public bool HasPreview => true;

	public string IconAutomationText { get; }

	public ImageMoniker IconMoniker { get; }

	public string InputGestureText { get; }

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
	{
		return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
	}

	public bool TryGetTelemetryId(out Guid telemetryId)
	{
		telemetryId = Guid.Empty;
		return false;
	}

	#endregion
}